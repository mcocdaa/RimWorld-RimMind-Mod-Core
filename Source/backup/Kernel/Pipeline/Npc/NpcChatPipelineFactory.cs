using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Result;
using RimMind.Kernel.Pipeline.Common;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;
using RimMind.Kernel.Pipeline;

namespace RimMind.Kernel.Pipeline.Npc
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
                    if (RimMindServiceLocator.Get<IRimMindRuntime>()?.IsShutdown == true)
                    {
                        ctx.ChatResult = Result<NpcChatResult, RimMindError>.Err(RimMindErrors.PipelineShortCircuited("RimMind is shut down."));
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
                    ctx.Items["telemetry.success"] = ctx.ChatResult?.IsOk ?? false;
                }, "Telemetry"),
                new NpcChatRetryMiddleware(),
                new StorageDriverInvokeMiddleware(),
            };

            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<NpcChatContext>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            return new Pipeline<NpcChatContext>(merged);
        }
    }
}
