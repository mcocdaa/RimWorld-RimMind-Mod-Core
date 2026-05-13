using System.Threading;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Pipeline.Npc
{
    public sealed class NpcChatContext : PipelineContextBase
    {
        public ContextRequest Request { get; set; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
        public Result<NpcChatResult, RimMindError>? ChatResult { get; set; }
        public new CancellationToken Ct { get; set; }
        public bool IsStreaming { get; set; }
        public System.Action<string>? OnStreamChunk { get; set; }

        public NpcChatContext() : base() { }
    }
}
