using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Pipeline.Npc
{
    internal sealed class NpcChatRetryMiddleware : IMiddleware<NpcChatContext>
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan[] Backoff = {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        };

        public string Id => Name;
        public string Name => nameof(NpcChatRetryMiddleware);
        public int Order => 6;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                context.ChatResult = null;
                await next(context);

                if (context.ChatResult?.IsOk == true)
                    return;

                if (context.ChatResult?.IsErr == true && !IsTransientError(context.ChatResult.Value.Error))
                    return;

                if (attempt < MaxRetries)
                    await Task.Delay(Backoff[attempt]);
            }
        }

        private static bool IsTransientError(RimMindError error)
        {
            return error.Code == RimMindErrorCode.ClientTransientFailure
                || error.Code == RimMindErrorCode.StorageDriverFailed
                || error.Code == RimMindErrorCode.Timeout;
        }
    }
}
