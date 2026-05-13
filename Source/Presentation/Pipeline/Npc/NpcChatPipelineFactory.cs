using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Models.Npc;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Pipeline.Npc
{
    public static class NpcChatPipelineFactory
    {
        public static IPipeline<NpcChatContext> Build(
            IExtensionRegistry<IMiddleware<NpcChatContext>>? extensions = null)
        {
            var defaults = new List<IMiddleware<NpcChatContext>>
            {
                new NpcAliveCheckMiddleware(),
                new StorageDriverInvokeMiddleware(),
            };

            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<NpcChatContext>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            var pipeline = new NpcChatPipeline();
            pipeline.UseRange(merged);
            return pipeline;
        }

        public static void Configure(NpcChatPipeline pipeline, RimMindRuntime runtime)
        {
            pipeline.Use(new NpcAliveCheckMiddleware());
            pipeline.Use(new StorageDriverInvokeMiddleware());
        }
    }
}
