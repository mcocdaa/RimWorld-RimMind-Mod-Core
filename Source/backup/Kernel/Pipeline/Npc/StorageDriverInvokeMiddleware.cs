using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Npc;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Result;

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
                NpcChatResult? aggregatedResult = null;
                await foreach (var chunk in driver.ChatStreamingAsync(npcId, "", query, context.OnStreamChunk, gameStateInfo, context.Ct))
                {
                    if (chunk.IsOk && chunk.Value.IsFinal)
                    {
                        aggregatedResult = new NpcChatResult(chunk.Value.NpcId, chunk.Value.Chunk, chunk.Value.Emotion)
                        {
                            AudioUrl = chunk.Value.AudioUrl
                        };
                    }
                    else if (chunk.IsErr)
                    {
                        context.ChatResult = Result<NpcChatResult, RimMindError>.Err(chunk.Error);
                        return;
                    }
                }
                if (aggregatedResult != null)
                    context.ChatResult = Result<NpcChatResult, RimMindError>.Ok(aggregatedResult);
            }
            else
            {
                context.ChatResult = await driver.ChatAsync(npcId, query, gameStateInfo);
            }
        }
    }
}
