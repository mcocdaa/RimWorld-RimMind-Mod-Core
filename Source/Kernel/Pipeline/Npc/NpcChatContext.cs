using System;
using System.Threading;
using RimMind.Contracts.Npc;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Pipeline.Npc
{
    public sealed class NpcChatContext : PipelineContextBase
    {
        public ContextRequest Request { get; set; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
        public Result<NpcChatResult, RimMindError>? ChatResult { get; set; }
        public CancellationToken Ct { get; set; }
        public bool IsStreaming { get; set; }
        public Action<string>? OnStreamChunk { get; set; }
    }
}
