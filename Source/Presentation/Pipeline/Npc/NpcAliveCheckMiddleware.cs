using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Features.Pipeline.Npc;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Pipeline.Npc
{
    internal sealed class NpcAliveCheckMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string OwnerModId => "RimMindCore";
        public string Name => nameof(NpcAliveCheckMiddleware);
        public int Order => 2;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var npcMgr = RimMindRuntime.Instance.GetAgentActionBridge() as INpcManager;
            if (context.Request?.NpcId != null && context.Request.NpcId.StartsWith("NPC-")
                && int.TryParse(context.Request.NpcId.Substring(4), out int pawnId))
            {
                // Use INpcManager.IsNpcAlive instead of direct Verse.Pawn.Dead access
                if (npcMgr != null && !npcMgr.IsNpcAlive(context.Request.NpcId))
                {
                    var profile = npcMgr.GetNpc(context.Request.NpcId);
                    if (profile != null)
                        npcMgr.SpawnNpc(profile);
                }
            }
            await next(context);
        }
    }
}
