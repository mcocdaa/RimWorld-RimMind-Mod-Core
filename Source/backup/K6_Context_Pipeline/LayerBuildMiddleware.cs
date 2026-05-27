using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Pipeline.Context
{
    internal sealed class LayerBuildMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Name => "ContextLayerBuild";
        public int Order => RimMindDefaults.MiddlewareOrder.LayerBuild;
        public string Id => "ContextLayerBuild";
        public string OwnerModId => "RimMindCore";

        private readonly IContextLayerBuilder _layerBuilder;
        private readonly IContextCacheManager _cacheManager;
        private readonly IContextDiffTracker _diffTracker;
        private readonly IContextKeyRegistry _keyRegistry;
        private readonly ILogSink? _log;

        public LayerBuildMiddleware(
            IContextLayerBuilder layerBuilder,
            IContextCacheManager cacheManager,
            IContextDiffTracker diffTracker,
            IContextKeyRegistry keyRegistry,
            ILogSink? log = null)
        {
            _layerBuilder = layerBuilder;
            _cacheManager = cacheManager;
            _diffTracker = diffTracker;
            _keyRegistry = keyRegistry;
            _log = log;
        }

        public async Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            if (!context.CacheHit && context.Snapshot != null)
            {
                _log?.Message($"[LayerBuild] Building layers for {context.Request.NpcId}");

                var allKeys = _keyRegistry.GetAll();
                var l0Keys = allKeys.FindAll(k => k.Layer == ContextLayer.L0_Static);
                var l1Keys = allKeys.FindAll(k => k.Layer == ContextLayer.L1_Baseline);

                var l0Msg = _layerBuilder.BuildL0(
                    context.Request.NpcId,
                    context.Request.Scenario,
                    l0Keys,
                    null,
                    _cacheManager);
                if (l0Msg != null)
                {
                    context.Snapshot.AddMessage(l0Msg);
                }

                var l1Msg = _layerBuilder.BuildL1(
                    context.Request.NpcId,
                    l1Keys,
                    null,
                    _cacheManager,
                    _diffTracker);
                if (l1Msg != null)
                {
                    context.Snapshot.AddMessage(l1Msg);
                }

                var l1DiffMsg = _layerBuilder.BuildDiffMessage(
                    context.Request.NpcId,
                    ContextLayer.L1_Baseline,
                    context.Snapshot,
                    _diffTracker);
                if (l1DiffMsg != null)
                {
                    context.Snapshot.AddMessage(l1DiffMsg);
                }
            }

            await next(context);
        }
    }
}
