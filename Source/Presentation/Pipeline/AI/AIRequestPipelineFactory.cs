using System.Collections.Generic;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;

namespace RimMind.Presentation.Pipeline.AI
{
    public static class AIRequestPipelineFactory
    {
        public static IPipeline<AIRequestContext> Build(
            ISettingsProvider settings,
            IExtensionRegistry<IMiddleware<AIRequestContext>>? extensions = null)
        {
            var defaults = new IMiddleware<AIRequestContext>[]
            {
                new ShortCircuitMiddleware(settings),
                new CircuitBreakerMiddleware(settings),
            };
            return PipelineFactory.Build(defaults, extensions);
        }
    }
}
