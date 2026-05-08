using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Npc;
using RimMind.Core.Pipeline.Npc;
using RimMind.Contracts.Npc;

namespace RimMind.Core.Pipeline.Npc
{
    internal sealed class StorageDriverInvokeMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(StorageDriverInvokeMiddleware);
        public int Order => 7;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var driver = StorageDriverFactory.GetDriver();
            var npcId = context.Request.NpcId;
            var query = context.Request.CurrentQuery ?? "";
            var ctx = "";
            context.Result = await driver.ChatAsync(npcId, query, ctx);
        }
    }
}
