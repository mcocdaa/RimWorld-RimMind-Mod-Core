using System;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Bus;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Abstractions;

namespace RimMind.Core.Pipeline.Bus
{
    internal sealed class ThreadAffinityCheckMiddleware<T> : IMiddleware<BusPublishContext<T>>
        where T : AgentBusEvent
    {
        public string Id => Name;
        public string Name => $"ThreadAffinityCheck_{typeof(T).Name}";
        public int Order => 0;

        public Task InvokeAsync(BusPublishContext<T> context, MiddlewareDelegate<BusPublishContext<T>> next)
        {
#if DEBUG
            var checker = RimMindServiceLocator.Get<IThreadChecker>();
            if (context.IsBackground)
            {
                if (checker != null && checker.IsMainThread)
                    throw new InvalidOperationException(
                        $"[RimMind-Core] BusPublish background context executed on main thread for {typeof(T).Name}");
            }
            else
            {
                if (checker != null && !checker.IsMainThread)
                    throw new InvalidOperationException(
                        $"[RimMind-Core] BusPublish main-thread context executed off main thread for {typeof(T).Name}");
            }
#endif
            return next(context);
        }
    }
}
