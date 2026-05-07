using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.AI;

namespace RimMind.Kernel.Pipeline.AI
{
    public static class AIRequestPipelineFactory
    {
        public static IPipeline<AIRequestContext> Build(IExtensionRegistry<IMiddleware<AIRequestContext>>? extensions = null)
        {
            var middlewares = new List<IMiddleware<AIRequestContext>>
            {
                new ShortCircuitMiddleware(),
                new TraceContextMiddleware(),
                new RequestSanitizeMiddleware(),
                new CacheMiddleware(),
                new TelemetryMiddleware(),
                new CircuitBreakerMiddleware(),
                new RetryMiddleware(),
                new ClientInvokeMiddleware(),
            };

            if (extensions != null)
            {
                middlewares.AddRange(extensions.All);
            }

            var sorted = middlewares.OrderBy(m => m.Order).ToList();
            return new Pipeline<AIRequestContext>(sorted);
        }
    }
}
