using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Common.Behaviours
{
    internal sealed class CommonRetryMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        public string Name => "CommonRetry";
        public int Order => 9000;
        public string Id => "CommonRetry";

        private readonly int _maxRetries;
        private readonly TimeSpan _delay;
        private readonly ILogSink? _log;

        public CommonRetryMiddleware(int maxRetries = 3, TimeSpan? delay = null, ILogSink? log = null)
        {
            _maxRetries = maxRetries;
            _delay = delay ?? TimeSpan.FromSeconds(1);
            _log = log;
        }

        public async Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    await next(context);
                    return;
                }
                catch (Exception ex) when (attempt < _maxRetries)
                {
                    attempt++;
                    _log?.Warning($"Retry {attempt}/{_maxRetries} after error: {ex.Message}");
                    await Task.Delay(_delay, context.Ct);
                }
            }
        }
    }
}
