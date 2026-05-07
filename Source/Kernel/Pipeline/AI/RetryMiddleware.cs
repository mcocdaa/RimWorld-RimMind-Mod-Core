using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.AI;
using RimMind.Kernel.Queue;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class RetryMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(RetryMiddleware);
        public int Order => 6;

        private const int MaxAttempts = 3;
        private static readonly int[] DelaySeconds = { 1, 2, 4 };

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                context.Error = null;

                await next(context).ConfigureAwait(false);

                if (context.Error == null)
                    return;

                if (!RetryPolicy.IsTransient(context.Error.Message))
                    return;

                if (attempt < MaxAttempts - 1)
                {
                    context.RetryCount++;
                    await Task.Delay(DelaySeconds[attempt] * 1000).ConfigureAwait(false);
                }
            }
        }
    }
}
