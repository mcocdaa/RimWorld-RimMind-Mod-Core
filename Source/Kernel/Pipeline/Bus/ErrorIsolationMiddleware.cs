using System;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.Bus;

namespace RimMind.Kernel.Pipeline.Bus
{
    internal sealed class ErrorIsolationMiddleware<T> : IMiddleware<BusPublishContext<T>>
        where T : AgentBusEvent
    {
        public string Id => Name;
        public string Name => $"ErrorIsolation_{typeof(T).Name}";
        public int Order => 2;

        public async Task InvokeAsync(BusPublishContext<T> context, MiddlewareDelegate<BusPublishContext<T>> next)
        {
            context.Items["error_isolation.enabled"] = true;
            await next(context);
        }
    }
}
