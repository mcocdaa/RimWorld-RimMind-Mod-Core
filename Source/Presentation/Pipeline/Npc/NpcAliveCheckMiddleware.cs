using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Runtime;
using Verse;

namespace RimMind.Presentation.Pipeline.Npc
{
    internal sealed class NpcAliveCheckMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(NpcAliveCheckMiddleware);
        public int Order => 2;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var npcMgr = RimMindRuntime.Instance.GetAgentActionBridge() as INpcManager;
            if (context.Request?.NpcId != null && context.Request.NpcId.StartsWith("NPC-")
                && int.TryParse(context.Request.NpcId.Substring(4), out int pawnId))
            {
                var pawn = npcMgr?.FindPawnByNpcId(context.Request.NpcId) as Pawn;
                if (pawn != null && pawn.Dead)
                {
                    var profile = NpcProfileBuilder.BuildPawnNpc(pawn);
                    npcMgr?.SpawnNpc(profile);
                }
            }
            await next(context);
        }
    }
}
