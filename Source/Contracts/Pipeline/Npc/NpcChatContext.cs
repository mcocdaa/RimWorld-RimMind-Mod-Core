using System;
using System.Threading;
using RimMind.Core.Npc;
using RimMind.Kernel.Context;
using RimMind.Contracts.Pipeline;

namespace RimMind.Contracts.Pipeline.Npc
{
    public sealed class NpcChatContext : PipelineContextBase
    {
        public ContextRequest Request { get; init; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
        public NpcChatResult? Result { get; set; }
        public CancellationToken Ct { get; init; }
        public bool IsStreaming { get; init; }
        public Action<string>? OnStreamChunk { get; init; }
    }
}
