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
            var envelope = context.Envelope;
            var npcId = envelope?.NpcId;
            if (npcId is { Length: > 0 } npcIdText)
            {
                if (_npcManager != null && !_npcManager.IsNpcAlive(npcIdText))
                {
                    var profile = _npcManager.GetNpc(npcIdText);
                    if (profile != null)
                    {
                        _log?.Message($"[UnifiedNpcEnrich] NPC {npcIdText} not alive, respawning");
                        _npcManager.SpawnNpc(profile);
                    }
                    else
                    {
                        _log?.Warning($"[UnifiedNpcEnrich] NPC {npcIdText} not found, cannot respawn");
                        context.Result = Result<LlmResponse, RimMindError>.Err(
                            RimMindErrors.NpcNotFound(npcIdText));
                        context.ShortCircuit("npc_not_found");
                        return;
                    }
                }

                // Inject game state info if not already set
                if (envelope!.GameStateInfo == null)
                {
                    var npcProfile = _npcManager?.GetNpc(npcIdText);
                    if (npcProfile != null)
                    {
                        context.Items["NpcProfile"] = npcProfile;
                    }
                }

                _log?.Message($"[UnifiedNpcEnrich] NPC {npcIdText} enriched");
            }

            await next(context);
        }
    }
}
