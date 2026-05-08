using System;
using System.Threading.Tasks;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Logging;

namespace RimMind.Core.Pipeline.Common
{
    public sealed class CommonRetryMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        private readonly Func<Exception, bool> _isTransient;
        private readonly int _maxRetries;
        private readonly TimeSpan _baseDelay;
        private readonly string _name;

        public CommonRetryMiddleware(
            Func<Exception, bool> isTransient,
            int maxRetries = 3,
            TimeSpan? baseDelay = null,
            string name = "Retry")
        {
            _isTransient = isTransient;
            _maxRetries = maxRetries;
            _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
            _name = name;
        }

        public string Id => $"Common.{_name}";
        public string Name => _name;
        public int Order => 800;

        public async Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    await next(context).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (_isTransient(ex) && attempt < _maxRetries)
                {
                    attempt++;
                    var delay = TimeSpan.FromTicks(_baseDelay.Ticks * (1L << (attempt - 1)));
                    RimMindLogger.Warning($"[{_name}] Attempt {attempt}/{_maxRetries} failed: {ex.Message}. Retry in {delay.TotalMilliseconds}ms");
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
        }
    }
}
