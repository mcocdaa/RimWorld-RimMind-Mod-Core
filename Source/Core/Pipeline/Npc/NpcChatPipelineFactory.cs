using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Npc;
using RimMind.Kernel.Pipeline;

namespace RimMind.Core.Pipeline.Npc
{
    public static class NpcChatPipelineFactory
    {
        public static IPipeline<NpcChatContext> Build(
            IExtensionRegistry<IMiddleware<NpcChatContext>>? extensions = null)
        {
            var defaults = new List<IMiddleware<NpcChatContext>>
            {
                new NpcChatShortCircuitMiddleware(),
                new NpcChatTraceContextMiddleware(),
                new NpcAliveCheckMiddleware(),
                new SnapshotBuildMiddleware(),
                new NpcChatTelemetryMiddleware(),
                new NpcChatRetryMiddleware(),
                new StorageDriverInvokeMiddleware(),
            };

            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<NpcChatContext>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            return new Pipeline<NpcChatContext>(merged);
        }
    }
}
