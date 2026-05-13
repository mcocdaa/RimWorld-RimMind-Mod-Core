using System;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.AI
{
    internal sealed class RetryMiddleware : IMiddleware<AIRequestContext>
    {
        public string Name => "AIRetry";
        public int Order => 800;
        public string Id => "AIRetry";

        private readonly int _maxRetries;
        private readonly TimeSpan _delay;
        private readonly ILogSink? _log;

        public RetryMiddleware(int maxRetries = 3, TimeSpan? delay = null, ILogSink? log = null)
        {
            _maxRetries = maxRetries;
            _delay = delay ?? TimeSpan.FromSeconds(2);
            _log = log;
        }

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            int maxAttempts = context.Request.MaxRetryCount ?? _maxRetries;
            for (int attempt = 0; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await next(context);
                    if (context.Response != null && context.Response.State != AIRequestState.Error)
                        return;
                }
                catch (Exception ex)
                {
                    _log?.Warning($"[AIRetry] Attempt {attempt + 1}/{maxAttempts + 1} failed: {ex.Message}");
                }

                if (attempt < maxAttempts)
                {
                    context.RetryCount = attempt + 1;
                    await Task.Delay(_delay, context.Ct);
                }
            }
        }
    }
}
