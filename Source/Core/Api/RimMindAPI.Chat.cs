using RimMind.Contracts;
using RimMind.Contracts.Context;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Result;
using RimMind.Core.Agent;
using RimMind.Core.Runtime;
using RimMind.Kernel.Pipeline.Npc;
using Verse;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RimMind.Core
{
    public static partial class RimMindAPI
    {
        public static class ChatFlow
        {
            public static async Task<Result<NpcChatResult, RimMindError>> Execute(ContextRequest request, CancellationToken ct = default)
            {
                if (RimMindRuntime.Instance.IsShutdown)
                    return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.PipelineShortCircuited("shutdown"));
                try
                {
                    var ctx = new NpcChatContext { Request = request, Ct = ct };
                    await RimMindRuntime.Instance.NpcChatPipeline.ExecuteAsync(ctx);
                    if (ctx.ChatResult.HasValue)
                        return ctx.ChatResult.Value;
                    if (ctx.IsShortCircuited)
                        return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.PipelineShortCircuited(ctx.ShortCircuitReason ?? "unknown"));
                    return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.Internal("Pipeline produced no result."));
                }
                catch (Exception ex)
                {
                    return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.Internal(ex.Message, ex));
                }
            }

            public static ContextSnapshot BuildContextSnapshot(ContextRequest request)
                => RimMindRuntime.Instance.ContextEngine.BuildSnapshot(request);

            public static string BuildMapContext(Map map, bool brief = false)
                => GameContextBuilder.BuildMapContext(map, brief);
        }
    }
}
