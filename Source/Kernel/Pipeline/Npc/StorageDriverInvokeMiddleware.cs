using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Npc;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Contracts.Npc;

namespace RimMind.Kernel.Pipeline.Npc
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
            var gameStateInfo = "";

            if (context.IsStreaming && driver.SupportsStreaming)
            {
                context.Result = await driver.ChatStreamingAsync(
                    npcId, "", query, context.OnStreamChunk, gameStateInfo, context.Ct);
            }
            else
            {
                context.Result = await driver.ChatAsync(npcId, query, gameStateInfo);
            }
        }
    }
}
