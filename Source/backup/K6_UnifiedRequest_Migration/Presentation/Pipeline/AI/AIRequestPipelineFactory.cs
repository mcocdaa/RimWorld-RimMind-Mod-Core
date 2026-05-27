using System.Collections.Generic;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Features.Pipeline.AI;
using RimMind.Application.Features.Tools;

namespace RimMind.Presentation.Pipeline.AI
{
    public static class AIRequestPipelineFactory
    {
        public static IPipeline<AIRequestContext> Build(
            ISettingsProvider settings,
            IToolRegistry? toolRegistry = null,
            ILogSink? logSink = null,
            IExtensionRegistry<IMiddleware<AIRequestContext>>? extensions = null)
        {
            var defaults = new List<IMiddleware<AIRequestContext>>
            {
                new ShortCircuitMiddleware(settings),
                new CircuitBreakerMiddleware(settings),
                new RequestSanitizeMiddleware(logSink),
                new CacheMiddleware(logSink),
                new ClientInvokeMiddleware(logSink),
                new RetryMiddleware(log: logSink),
            };

            if (toolRegistry != null)
            {
                defaults.Add(new ToolCallDispatchMiddleware(toolRegistry, logSink));
            }

            return PipelineFactory.Build(defaults, extensions);
        }
    }
}
