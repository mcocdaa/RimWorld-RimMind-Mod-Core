using System;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Bus;
using RimMind.Kernel.Logging;
using RimMind.Contracts.Result;

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
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var error = RimMindErrors.Internal(ex.Message, ex);
                context.HandlerErrors.Add(error);
                RimMindLogger.Warning($"[ErrorIsolation] Isolated error in {typeof(T).Name} pipeline: {ex.Message}");
            }
        }
    }
}
