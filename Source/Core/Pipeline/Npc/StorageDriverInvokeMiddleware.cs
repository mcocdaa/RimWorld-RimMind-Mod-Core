using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Npc;
using RimMind.Core.Npc;

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
            if (context.IsStreaming)
            {
                context.Result = await driver.ChatStreamingAsync(
                    context.Request.NpcId,
                    context.Request.SpeakerName ?? "",
                    context.Request.CurrentQuery ?? "",
                    context.OnStreamChunk,
                    ct: context.Ct);
            }
            else
            {
                context.Result = await driver.ChatAsync(context.Snapshot!, context.Ct);
            }
        }
    }
}
