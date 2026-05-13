using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;

namespace RimMind.Kernel.Pipeline.Common
{
    public sealed class CommonTelemetryMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        private readonly Action<TContext, TimeSpan, string?> _recordTelemetry;
        private readonly string _name;

        public CommonTelemetryMiddleware(Action<TContext, TimeSpan, string?> recordTelemetry, string name = "Telemetry")
        {
            _recordTelemetry = recordTelemetry;
            _name = name;
        }

        public string Id => $"Common.{_name}";
        public string Name => _name;
        public int Order => -200;

        public async Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            var sw = Stopwatch.StartNew();
            string? error = null;
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                throw;
            }
            finally
            {
                sw.Stop();
                _recordTelemetry(context, sw.Elapsed, error);
            }
        }
    }
}
