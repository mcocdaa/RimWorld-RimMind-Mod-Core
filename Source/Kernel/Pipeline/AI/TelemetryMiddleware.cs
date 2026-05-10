using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.AI;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Logging;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class TelemetryMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(TelemetryMiddleware);
        public int Order => -200;

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
                    PromptTokens = context.Result?.Match(ok => ok.PromptTokens, _ => 0) ?? 0,
                    CompletionTokens = context.Result?.Match(ok => ok.CompletionTokens, _ => 0) ?? 0,
                    TotalTokens = context.Result?.Match(ok => ok.TokensUsed, _ => 0) ?? 0,
                    CachedTokens = context.Result?.Match(ok => ok.CachedTokens, _ => 0) ?? 0,
                    TraceId = RimMindLogger.CurrentTraceId,
                    RequestLatencyMs = sw.ElapsedMilliseconds,
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    ResponseParseSuccess = context.Result?.IsOk ?? false,
                };

                try
                {
                    RimMindServiceLocator.Get<IRimMindRuntime>();
                }
                catch
                {
                }
            }
        }
    }
}
