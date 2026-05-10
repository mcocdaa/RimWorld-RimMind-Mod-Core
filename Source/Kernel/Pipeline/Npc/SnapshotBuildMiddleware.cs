using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Kernel.Context;
using RimMind.Contracts.Internal;

namespace RimMind.Kernel.Pipeline.Npc
{
    internal sealed class SnapshotBuildMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(SnapshotBuildMiddleware);
        public int Order => 3;

        public Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var engine = RimMindServiceLocator.Get<IContextEngine>();
            var snapshot = engine.BuildSnapshot(context.Request);
            context.Snapshot = snapshot;
            return next(context);
        }
    }
}
