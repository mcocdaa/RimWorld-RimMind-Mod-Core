using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.Npc;
using RimMind.Core.Runtime;

namespace RimMind.Kernel.Pipeline.Npc
{
    internal sealed class SnapshotBuildMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(SnapshotBuildMiddleware);
        public int Order => 3;

        public Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var snapshot = RimMindRuntime.Instance.ContextEngine.BuildSnapshot(context.Request);
            context.Snapshot = snapshot;
            return next(context);
        }
    }
}
