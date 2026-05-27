using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Features.Pipeline.Npc;

namespace RimMind.Presentation.Pipeline.Npc
{
    public static class NpcChatPipelineFactory
    {
        public static IPipeline<NpcChatContext> Build(
            IContextBuilder? contextBuilder = null,
            IStorageDriverFactory? storageDriverFactory = null,
            ILogSink? logSink = null,
            INpcManager? npcManager = null,
            IExtensionRegistry<IMiddleware<NpcChatContext>>? extensions = null)
        {
            var defaults = new IMiddleware<NpcChatContext>[]
            {
                new NpcAliveCheckMiddleware(npcManager),
                new SnapshotBuildMiddleware(contextBuilder, logSink),
                new StorageDriverInvokeMiddleware(storageDriverFactory, logSink),
                new NpcChatRetryMiddleware(log: logSink),
            };
            return PipelineFactory.Build(defaults, extensions);
        }
    }
}
