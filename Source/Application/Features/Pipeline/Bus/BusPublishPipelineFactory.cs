using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Bus
{
    internal static class BusPublishPipelineFactory
    {
        public static IPipeline<BusPublishContext> Build(
            IExtensionRegistry<IMiddleware<BusPublishContext>>? extensions = null)
        {
            var defaults = new IMiddleware<BusPublishContext>[]
            {
                new BusPublishTelemetryMiddleware(),
                new ThreadAffinityCheckMiddleware(),
                new ErrorIsolationMiddleware(),
                new DispatchMiddleware(
                    RimMindServiceLocator.Get<IAgentBus>()!,
                    RimMindServiceLocator.Get<ILogSink>()),
            };
            return PipelineFactory.Build(defaults, extensions);
        }
    }
}
