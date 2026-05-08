using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Context;

namespace RimMind.Core.Pipeline.Context
{
    internal sealed class ContextBuildTelemetryMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Id => Name;
        public string Name => nameof(ContextBuildTelemetryMiddleware);
        public int Order => 4;

        public async Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
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
                context.Items["telemetry.estimated_tokens"] = context.Snapshot?.EstimatedTokens ?? 0;
            }
        }
    }
}
