using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Npc;
using RimMind.Kernel.Logging;

namespace RimMind.Core.Pipeline.Npc
{
    internal sealed class NpcChatTraceContextMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(NpcChatTraceContextMiddleware);
        public int Order => 1;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            using (RimMindLogger.BeginTraceScope(context.TraceId))
            {
                await next(context);
            }
        }
    }
}
