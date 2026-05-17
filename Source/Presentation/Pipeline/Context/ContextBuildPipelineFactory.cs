using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;

namespace RimMind.Presentation.Pipeline.Context
{
    public static class ContextBuildPipelineFactory
    {
        public static IPipeline<ContextBuildContext> Build(
            ISettingsProvider settings,
            IExtensionRegistry<IMiddleware<ContextBuildContext>>? extensions = null)
        {
            var defaults = new IMiddleware<ContextBuildContext>[]
            {
                new BudgetTrimMiddleware(settings, settings),
            };
            return PipelineFactory.Build(defaults, extensions);
        }
    }
}
