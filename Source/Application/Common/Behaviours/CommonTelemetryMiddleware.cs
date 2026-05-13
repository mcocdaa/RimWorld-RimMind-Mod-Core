using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Common.Behaviours
{
    internal sealed class CommonTelemetryMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        public string Name => "CommonTelemetry";
        public int Order => int.MinValue;
        public string Id => "CommonTelemetry";

        private readonly ILogSink? _log;

        public CommonTelemetryMiddleware(ILogSink? log = null)
        {
            _log = log;
        }

        public async Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            var start = DateTime.UtcNow;
            try
            {
                await next(context);
            }
            finally
            {
                var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                _log?.Message($"[Telemetry] Pipeline {context.TraceId} completed in {elapsed:F0}ms" +
                    (context.IsShortCircuited ? $" (short-circuited: {context.ShortCircuitReason})" : ""));
            }
        }
    }
}
