using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Features.Flywheel;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;

namespace RimMind.Presentation.Pipeline.AI
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
                new CircuitBreakerMiddleware(),
            };

            if (extensions != null)
            {
                middlewares.AddRange(extensions.All);
            }

            var sorted = middlewares.OrderBy(m => m.Order).ToList();
            var pipeline = new AIRequestPipeline();
            pipeline.UseRange(sorted);
            return pipeline;
        }

        public static void Configure(AIRequestPipeline pipeline, RimMindRuntime runtime)
        {
            pipeline.Use(new ShortCircuitMiddleware());
            pipeline.Use(new CircuitBreakerMiddleware());
        }
    }
}
