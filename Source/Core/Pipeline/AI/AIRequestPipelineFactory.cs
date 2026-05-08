using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Client;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Common;
using RimMind.Core.Runtime;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Logging;
using RimMind.Kernel.Pipeline;

namespace RimMind.Core.Pipeline.AI
{
    public static class AIRequestPipelineFactory
    {
        public static IPipeline<AIRequestContext> Build(IExtensionRegistry<IMiddleware<AIRequestContext>>? extensions = null)
        {
            var middlewares = new List<IMiddleware<AIRequestContext>>
            {
                new CommonShortCircuitMiddleware<AIRequestContext>(ctx =>
                {
                    if (RimMindRuntime.Instance.IsShutdown)
                    {
                        ctx.Response = AIResponse.Failure(ctx.Request.RequestId, "shutdown");
                        return "shutdown";
                    }
                    if (RimMindCoreMod.Settings?.IsConfigured() != true)
                    {
                        ctx.Response = AIResponse.Failure(ctx.Request.RequestId, "not_configured");
                        return "not_configured";
                    }
                    if (ctx.Client == null)
                    {
                        ctx.Response = AIResponse.Failure(ctx.Request.RequestId, "no_client");
                        return "no_client";
                    }
                    return null;
                }, "ShortCircuit"),
                new CommonTraceContextMiddleware<AIRequestContext>(),
                new RequestSanitizeMiddleware(),
                new CacheMiddleware(),
                new CommonTelemetryMiddleware<AIRequestContext>((ctx, elapsed, err) =>
                {
                    ctx.Elapsed = elapsed;
                    var record = new TelemetryRecord
                    {
                        NpcId = ctx.Request.ModId,
                        Scenario = ctx.Request.RequestId,
                        PromptTokens = ctx.Response?.PromptTokens ?? 0,
                        CompletionTokens = ctx.Response?.CompletionTokens ?? 0,
                        TotalTokens = ctx.Response?.TokensUsed ?? 0,
                        CachedTokens = ctx.Response?.CachedTokens ?? 0,
                        BudgetValue = ctx.Snapshot?.BudgetValue ?? 0,
                        KeysIncluded = ctx.Snapshot?.IncludedKeys,
                        KeysTrimmed = ctx.Snapshot?.TrimmedKeys,
                        LayerTokenBreakdown = ctx.Snapshot != null ? new Dictionary<string, int>
                        {
                            { "L0", ctx.Snapshot.Meta.L0Tokens }, { "L1", ctx.Snapshot.Meta.L1Tokens },
                            { "L2", ctx.Snapshot.Meta.L2Tokens }, { "L3", ctx.Snapshot.Meta.L3Tokens },
                            { "L4", ctx.Snapshot.Meta.L4Tokens }, { "L5", ctx.Snapshot.Meta.L5Tokens },
                        } : null,
                        KeyChangeFreq = ctx.Snapshot?.KeyChangeCounts.Count > 0 ? new Dictionary<string, int>(ctx.Snapshot.KeyChangeCounts) : null,
                        ScoreDistribution = ctx.Snapshot?.KeyScores.Count > 0 ? new Dictionary<string, float>(ctx.Snapshot.KeyScores) : null,
                        DiffCount = ctx.Snapshot?.DiffCount ?? 0,
                        LatencyByLayerMs = ctx.Snapshot?.LatencyByLayerMs.Count > 0 ? new Dictionary<string, long>(ctx.Snapshot.LatencyByLayerMs) : null,
                        TraceId = RimMindLogger.CurrentTraceId,
                        RequestLatencyMs = elapsed.Milliseconds,
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        ResponseParseSuccess = ctx.Response?.Success ?? false,
                    };
                    try { RimMindRuntime.Instance.Telemetry.Record(record); } catch { }
                }, "Telemetry"),
                new CircuitBreakerMiddleware(),
                new RetryMiddleware(),
                new ClientInvokeMiddleware(),
            };

            if (extensions != null)
            {
                middlewares.AddRange(extensions.All);
            }

            var sorted = middlewares.OrderBy(m => m.Order).ToList();
            return new Pipeline<AIRequestContext>(sorted);
        }
    }
}
