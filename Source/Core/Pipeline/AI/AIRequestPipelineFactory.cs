using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Core.Client;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Common;
using RimMind.Core.Runtime;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Logging;
using RimMind.Kernel.Pipeline;
using RimMind.Kernel.Queue;

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
                        TraceId = RimMindLogger.CurrentTraceId,
                        RequestLatencyMs = elapsed.Milliseconds,
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        ResponseParseSuccess = ctx.Response?.Success ?? false,
                    };
                    try { RimMindRuntime.Instance.Telemetry.Record(record); } catch { }
                }, "Telemetry"),
                new CircuitBreakerMiddleware(),
                new CommonRetryMiddleware<AIRequestContext>(
                    ex => RetryPolicy.IsTransient(ex.Message),
                    3,
                    TimeSpan.FromSeconds(1),
                    "Retry"),
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
