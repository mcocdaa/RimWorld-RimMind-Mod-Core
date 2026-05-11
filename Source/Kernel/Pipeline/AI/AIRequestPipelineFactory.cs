using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts;
using RimMind.Contracts.Client;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Tools;
using RimMind.Kernel.Pipeline.Common;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Logging;
using RimMind.Kernel.Pipeline;
using RimMind.Kernel.Tools;

namespace RimMind.Kernel.Pipeline.AI
{
    public static class AIRequestPipelineFactory
    {
        public static IPipeline<AIRequestContext> Build(
            IToolRegistry? toolRegistry = null,
            IExtensionRegistry<IMiddleware<AIRequestContext>>? extensions = null,
            Func<int>? getMaxDepth = null,
            IAgentBus? bus = null)
        {
            var middlewares = new List<IMiddleware<AIRequestContext>>
            {
                new ShortCircuitMiddleware(),
                new CommonTraceContextMiddleware<AIRequestContext>(),
                new RequestSanitizeMiddleware(),
                new CacheMiddleware(),
                new CommonTelemetryMiddleware<AIRequestContext>((ctx, elapsed, err) =>
                {
                    ctx.Elapsed = elapsed;
                }, "Telemetry"),
                new CircuitBreakerMiddleware(),
                new RetryMiddleware(),
            };

            if (toolRegistry != null)
            {
                middlewares.Add(new ToolCallDispatchMiddleware(toolRegistry, bus, getMaxDepth));
            }

            middlewares.Add(new ClientInvokeMiddleware());

            if (extensions != null)
            {
                middlewares.AddRange(extensions.All);
            }

            var sorted = middlewares.OrderBy(m => m.Order).ToList();
            return new Pipeline<AIRequestContext>(sorted);
        }
    }
}
