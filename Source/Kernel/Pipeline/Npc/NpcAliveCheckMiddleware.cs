using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Result;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Kernel.Queue;
using RimMind.Core.Npc;
using RimMind.Core.Agent;
using Verse;

namespace RimMind.Kernel.Pipeline.Npc
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
                var pawn = npcMgr?.FindPawnByNpcId(context.Request.NpcId) as Pawn;
                if (pawn != null)
                {
                    var profile = NpcProfileBuilder.BuildPawnNpc(pawn);
                    var spawnResult = await driver.SpawnNpcAsync(profile);
                    if (spawnResult.IsErr)
                        AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] NpcAliveCheck: SpawnNpc failed: {spawnResult.Error}", isWarning: true);
                    LongEventHandler.ExecuteWhenFinished(() => npcMgr?.SpawnNpc(profile));
                }
            }
            await next(context);
        }
    }
}
