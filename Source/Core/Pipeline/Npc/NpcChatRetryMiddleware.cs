using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Npc;
using RimMind.Core.Pipeline.Npc;
using RimMind.Contracts.Npc;

namespace RimMind.Core.Pipeline.Npc
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
                try
                {
                    await next(context);
                    if (context.Result?.Error == null || !IsTransientError(context.Result.Error))
                        return;
                }
                catch (Exception ex) when (TransientExceptionChecker.IsTransient(ex) && attempt < MaxRetries)
                {
                }
                if (attempt < MaxRetries)
                    await Task.Delay(Backoff[attempt]);
            }
        }

        private static bool IsTransientError(string error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            return error.Contains("429") || error.Contains("503") || error.Contains("504")
                || error.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || error.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
        }
    }
}
