using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Pipeline.Unified
{
    internal sealed class NpcEnrichMiddleware : IMiddleware<LlmRequestContext>
    {
        public string Name => "UnifiedNpcEnrich";
        public int Order => RimMindDefaults.MiddlewareOrder.NpcEnrich;
        public string Id => "UnifiedNpcEnrich";
        public string OwnerModId => "RimMindCore";

        private readonly INpcManager? _npcManager;
        private readonly ILogSink? _log;

        public NpcEnrichMiddleware(INpcManager? npcManager = null, ILogSink? log = null)
        {
            _npcManager = npcManager;
            _log = log;
        }

        public async Task InvokeAsync(LlmRequestContext context, MiddlewareDelegate<LlmRequestContext> next)
        {
            var npcId = context.Envelope?.NpcId;
            if (!string.IsNullOrEmpty(npcId))
            {
                if (_npcManager != null && !_npcManager.IsNpcAlive(npcId))
                {
                    var profile = _npcManager.GetNpc(npcId);
                    if (profile != null)
                    {
                        _log?.Message($"[UnifiedNpcEnrich] NPC {npcId} not alive, respawning");
                        _npcManager.SpawnNpc(profile);
                    }
                    else
                    {
                        _log?.Warning($"[UnifiedNpcEnrich] NPC {npcId} not found, cannot respawn");
                        context.Result = Result<LlmResponse, RimMindError>.Err(
                            RimMindErrors.NpcNotFound(npcId));
                        context.ShortCircuit("npc_not_found");
                        return;
                    }
                }

                // Inject game state info if not already set
                if (string.IsNullOrEmpty(context.Envelope.GameStateInfo))
                {
                    var npcProfile = _npcManager?.GetNpc(npcId);
                    if (npcProfile != null)
                    {
                        context.Items["NpcProfile"] = npcProfile;
                    }
                }

                _log?.Message($"[UnifiedNpcEnrich] NPC {npcId} enriched");
            }

            await next(context);
        }
    }
}
