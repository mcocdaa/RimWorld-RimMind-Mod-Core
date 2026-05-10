using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Pipeline.Npc
{
    internal sealed class NpcChatShortCircuitMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(NpcChatShortCircuitMiddleware);
        public int Order => 0;

        public Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            if (RimMindServiceLocator.Get<IRimMindRuntime>()?.IsShutdown == true)
            {
                context.ChatResult = Result<NpcChatResult, RimMindError>.Err(RimMindErrors.PipelineShortCircuited("RimMind is shut down."));
                context.ShortCircuit("shutdown");
                return Task.CompletedTask;
            }
            return next(context);
        }
    }
}
