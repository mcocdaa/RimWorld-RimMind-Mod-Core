using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Context
{
    internal sealed class LayerBuildMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Name => "ContextLayerBuild";
        public int Order => 300;
        public string Id => "ContextLayerBuild";

        private readonly IContextLayerBuilder _layerBuilder;
        private readonly ILogSink? _log;

        public LayerBuildMiddleware(IContextLayerBuilder layerBuilder, ILogSink? log = null)
        {
            _layerBuilder = layerBuilder;
            _log = log;
        }

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            if (context.Snapshot != null && !context.CacheHit)
            {
                _log?.Message($"[LayerBuild] Building layers for {context.Request.NpcId}");
            }
            return next(context);
        }
    }
}
