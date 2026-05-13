using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.AI;
using RimMind.Kernel.Queue;
using RimMind.Contracts.Result;

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
                context.Result = null;

                await next(context).ConfigureAwait(false);

                if (context.Result != null && context.Result.Value.IsOk)
                    context.Result.Value.Value.AttemptCount = attempt + 1;

                if (context.Result?.IsOk == true)
                    return;

                if (context.Result?.IsErr == true && !IsTransientError(context.Result.Value.Error))
                    return;

                if (attempt < MaxAttempts - 1)
                {
                    context.RetryCount++;
                    await Task.Delay(DelaySeconds[attempt] * 1000).ConfigureAwait(false);
                }
            }
        }

        private static bool IsTransientError(RimMindError error)
        {
            return error.Code == RimMindErrorCode.ClientTransientFailure;
        }
    }
}
