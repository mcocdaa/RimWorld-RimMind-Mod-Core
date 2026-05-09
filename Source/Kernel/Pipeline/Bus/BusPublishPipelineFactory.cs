using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Bus;
using RimMind.Kernel.Pipeline;

namespace RimMind.Core.Pipeline.Bus
{
    public static class BusPublishPipelineFactory<T> where T : AgentBusEvent
    {
        public static IPipeline<BusPublishContext<T>> Build(
            IExtensionRegistry<IMiddleware<BusPublishContext<T>>>? extensions = null)
        {
            var defaults = new List<IMiddleware<BusPublishContext<T>>>
            {
                new ThreadAffinityCheckMiddleware<T>(),
                new BusPublishTelemetryMiddleware<T>(),
                new ErrorIsolationMiddleware<T>(),
                new DispatchMiddleware<T>(),
            };

            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<BusPublishContext<T>>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            return new Pipeline<BusPublishContext<T>>(merged);
        }
    }
}
