using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Context;
using RimMind.Kernel.Context;

namespace RimMind.Core.Pipeline.Context
{
    internal sealed class LayerBuildMiddleware : IMiddleware<ContextBuildContext>
    {
        private readonly ContextOrchestrator _orchestrator;

        public LayerBuildMiddleware(ContextOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public string Id => Name;
        public string Name => nameof(LayerBuildMiddleware);
        public int Order => 1;

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            var snapshot = _orchestrator.BuildSnapshotCore(context.Request);
            context.Snapshot = snapshot;
            return next(context);
        }
    }
}
