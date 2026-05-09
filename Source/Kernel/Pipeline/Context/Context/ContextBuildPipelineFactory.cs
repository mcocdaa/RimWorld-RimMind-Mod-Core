using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Context;
using RimMind.Kernel.Context;
using RimMind.Kernel.Pipeline;

namespace RimMind.Core.Pipeline.Context
{
    public static class ContextBuildPipelineFactory
    {
        public static IPipeline<ContextBuildContext> Build(
            ContextOrchestrator orchestrator,
            IContextCacheManager cacheManager,
            IExtensionRegistry<IMiddleware<ContextBuildContext>>? extensions = null)
        {
            var defaults = new List<IMiddleware<ContextBuildContext>>
            {
                new CacheLookupMiddleware(cacheManager),
                new LayerBuildMiddleware(orchestrator),
                new BudgetTrimMiddleware(),
                new CacheStoreMiddleware(cacheManager),
                new ContextBuildTelemetryMiddleware(),
            };

            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<ContextBuildContext>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            return new Pipeline<ContextBuildContext>(merged);
        }
    }
}
