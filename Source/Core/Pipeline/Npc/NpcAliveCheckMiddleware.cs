using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Internal;
using RimMind.Core.Pipeline.Npc;
using RimMind.Core.Npc;
using Verse;

namespace RimMind.Core.Pipeline.Npc
{
    internal sealed class NpcAliveCheckMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(NpcAliveCheckMiddleware);
        public int Order => 2;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var driver = StorageDriverFactory.GetDriver();
            if (!driver.IsNpcAlive(context.Request.NpcId)
                && context.Request.NpcId.StartsWith("NPC-")
                && int.TryParse(context.Request.NpcId.Substring(4), out _))
            {
                var npcMgr = RimMindServiceLocator.Get<INpcManager>();
                var pawn = npcMgr?.FindPawnByNpcId(context.Request.NpcId);
                if (pawn != null)
                {
                    var profile = NpcProfileBuilder.BuildPawnNpc(pawn);
                    await driver.SpawnNpcAsync(profile);
                    LongEventHandler.ExecuteWhenFinished(() => npcMgr?.SpawnNpc(profile));
                }
            }
            await next(context);
        }
    }
}
