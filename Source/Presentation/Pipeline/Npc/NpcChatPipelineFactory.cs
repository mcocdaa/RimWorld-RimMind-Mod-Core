using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Presentation.Pipeline.Npc
{
    public static class NpcChatPipelineFactory
    {
        public static IPipeline<NpcChatContext> Build(
            IExtensionRegistry<IMiddleware<NpcChatContext>>? extensions = null)
        {
            var defaults = new IMiddleware<NpcChatContext>[]
            {
                new NpcAliveCheckMiddleware(),
                new StorageDriverInvokeMiddleware(),
            };
            return PipelineFactory.Build(defaults, extensions);
        }
    }
}
