using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.AI;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Logging;
using RimMind.Core.Runtime;

namespace RimMind.Core.Pipeline.AI
{
    public sealed class TelemetryMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(TelemetryMiddleware);
        public int Order => 4;

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                sw.Stop();
                context.Elapsed = sw.Elapsed;

                var record = new TelemetryRecord
                {
                    NpcId = context.Request.ModId,
                    Scenario = context.Request.RequestId,
                    PromptTokens = context.Response?.PromptTokens ?? 0,
                    CompletionTokens = context.Response?.CompletionTokens ?? 0,
                    TotalTokens = context.Response?.TokensUsed ?? 0,
                    CachedTokens = context.Response?.CachedTokens ?? 0,
                    TraceId = RimMindLogger.CurrentTraceId,
                    RequestLatencyMs = sw.ElapsedMilliseconds,
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    ResponseParseSuccess = context.Response?.Success ?? false,
                };

                try
                {
                    RimMindRuntime.Instance.Telemetry.Record(record);
                }
                catch
                {
                }
            }
        }
    }
}
