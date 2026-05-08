using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Npc;
using RimMind.Core.Npc;
using RimMind.Core.Pipeline.Common;
using RimMind.Core.Runtime;
using RimMind.Kernel.Pipeline;

namespace RimMind.Core.Pipeline.Npc
{
    public static class NpcChatPipelineFactory
    {
        public static IPipeline<NpcChatContext> Build(
            IExtensionRegistry<IMiddleware<NpcChatContext>>? extensions = null)
        {
            var defaults = new List<IMiddleware<NpcChatContext>>
            {
                new CommonShortCircuitMiddleware<NpcChatContext>(ctx =>
                {
                    if (RimMindRuntime.Instance.IsShutdown)
                    {
                        ctx.Result = new NpcChatResult { Error = "RimMind is shut down." };
                        return "shutdown";
                    }
                    return null;
                }, "ShortCircuit"),
                new CommonTraceContextMiddleware<NpcChatContext>(),
                new NpcAliveCheckMiddleware(),
                new SnapshotBuildMiddleware(),
                new CommonTelemetryMiddleware<NpcChatContext>((ctx, elapsed, err) =>
                {
                    ctx.Items["telemetry.elapsed_ms"] = elapsed.TotalMilliseconds;
                    ctx.Items["telemetry.npc_id"] = ctx.Request.NpcId;
                    ctx.Items["telemetry.scenario"] = ctx.Request.Scenario;
                    ctx.Items["telemetry.success"] = ctx.Result?.Error == null;
                }, "Telemetry"),
                new CommonRetryMiddleware<NpcChatContext>(
                    ex => TransientExceptionChecker.IsTransient(ex),
                    3,
                    TimeSpan.FromSeconds(1),
                    "Retry"),
                new StorageDriverInvokeMiddleware(),
            };

            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<NpcChatContext>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            return new Pipeline<NpcChatContext>(merged);
        }
    }
}
