using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.Npc;
using RimMind.Core.Npc;
using RimMind.Core.Runtime;

namespace RimMind.Kernel.Pipeline.Npc
{
    internal sealed class NpcChatShortCircuitMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(NpcChatShortCircuitMiddleware);
        public int Order => 0;

        public Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            if (RimMindRuntime.Instance.IsShutdown)
            {
                context.Result = new NpcChatResult { Error = "RimMind is shut down." };
                context.ShortCircuit("shutdown");
                return Task.CompletedTask;
            }
            return next(context);
        }
    }
}
