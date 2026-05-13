using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation.Context;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Pipeline.Context
{
    public static class ContextBuildPipelineFactory
    {
        public static IPipeline<ContextBuildContext> Build(
            ContextOrchestrator orchestrator,
            IExtensionRegistry<IMiddleware<ContextBuildContext>>? extensions = null)
        {
            var defaults = new List<IMiddleware<ContextBuildContext>>
            {
                new BudgetTrimMiddleware(),
            };

            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<ContextBuildContext>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            var pipeline = new ContextBuildPipeline();
            pipeline.UseRange(merged);
            return pipeline;
        }

        public static void Configure(ContextBuildPipeline pipeline, RimMindRuntime runtime)
        {
            pipeline.Use(new BudgetTrimMiddleware());
        }
    }
}
