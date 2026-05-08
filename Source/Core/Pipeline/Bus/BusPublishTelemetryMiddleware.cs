using System;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Bus;

namespace RimMind.Core.Pipeline.Bus
{
    internal sealed class BusPublishTelemetryMiddleware<T> : IMiddleware<BusPublishContext<T>>
        where T : AgentBusEvent
    {
        public string Id => Name;
        public string Name => $"Telemetry_{typeof(T).Name}";
        public int Order => -200;

        public async Task InvokeAsync(BusPublishContext<T> context, MiddlewareDelegate<BusPublishContext<T>> next)
        {
            var start = DateTime.UtcNow;
            try
            {
                await next(context);
            }
            finally
            {
                var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                context.Items["telemetry.elapsed_ms"] = elapsed;
                context.Items["telemetry.event_type"] = typeof(T).Name;
                context.Items["telemetry.handler_count"] = context.Subscribers.Count;
                context.Items["telemetry.error_count"] = context.HandlerErrors.Count;
            }
        }
    }
}
