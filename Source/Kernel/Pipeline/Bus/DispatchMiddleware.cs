using System;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.Bus;
using RimMind.Kernel.Logging;

namespace RimMind.Kernel.Pipeline.Bus
{
    internal sealed class DispatchMiddleware<T> : IMiddleware<BusPublishContext<T>>
        where T : AgentBusEvent
    {
        public string Id => Name;
        public string Name => $"Dispatch_{typeof(T).Name}";
        public int Order => 3;

        public Task InvokeAsync(BusPublishContext<T> context, MiddlewareDelegate<BusPublishContext<T>> next)
        {
            foreach (var subscriber in context.Subscribers)
            {
                try
                {
                    if (subscriber is Action<T> action)
                        action(context.Event);
                }
                catch (Exception ex)
                {
                    context.HandlerErrors.Add(ex);
                    RimMindLogger.Warning($"AgentBus handler error for {typeof(T).Name}: {ex.Message}");
                }
            }
            return Task.CompletedTask;
        }
    }
}
