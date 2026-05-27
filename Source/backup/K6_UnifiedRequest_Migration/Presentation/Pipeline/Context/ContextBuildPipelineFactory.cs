using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Pipeline.Context;

namespace RimMind.Presentation.Pipeline.Context
{
    public static class ContextBuildPipelineFactory
    {
        public static IPipeline<ContextBuildContext> Build(
            ISettingsProvider settings,
            IContextCacheManager? cacheManager = null,
            IContextLayerBuilder? layerBuilder = null,
            IContextDiffTracker? diffTracker = null,
            IContextKeyRegistry? keyRegistry = null,
            ILogSink? logSink = null,
            IExtensionRegistry<IMiddleware<ContextBuildContext>>? extensions = null)
        {
            var defaults = new System.Collections.Generic.List<IMiddleware<ContextBuildContext>>
            {
                new ContextBuildTelemetryMiddleware(logSink),
                new BudgetTrimMiddleware(settings, settings),
            };

            if (cacheManager != null)
            {
                defaults.Add(new CacheLookupMiddleware(cacheManager, logSink));
            }

            if (layerBuilder != null && cacheManager != null && diffTracker != null && keyRegistry != null)
            {
                defaults.Add(new LayerBuildMiddleware(layerBuilder, cacheManager, diffTracker, keyRegistry, logSink));
            }

            if (cacheManager != null)
            {
                defaults.Add(new CacheStoreMiddleware(cacheManager, logSink));
            }

            return PipelineFactory.Build(defaults, extensions);
        }
    }
}
