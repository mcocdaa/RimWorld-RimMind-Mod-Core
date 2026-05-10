using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;

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
                context.Result = new NpcChatResult { Error = "RimMind is shut down." };
                context.ShortCircuit("shutdown");
                return Task.CompletedTask;
            }
            return next(context);
        }
    }
}
