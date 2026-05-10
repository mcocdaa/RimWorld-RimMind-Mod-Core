using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Npc;

namespace RimMind.Kernel.Pipeline.Npc
{
    internal sealed class NpcChatTelemetryMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string Name => nameof(NpcChatTelemetryMiddleware);
        public int Order => -200;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var start = DateTime.UtcNow;
            try
            {
                await next(context);
            }
            finally
            {
                var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                context.Items["telemetry.elapsed_ms"] = elapsed;
                context.Items["telemetry.npc_id"] = context.Request.NpcId;
                context.Items["telemetry.scenario"] = context.Request.Scenario;
                context.Items["telemetry.success"] = context.ChatResult?.IsOk ?? false;
            }
        }
    }
}
