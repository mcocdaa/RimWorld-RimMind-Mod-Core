using System.Threading;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Pipeline.Npc
{
    public sealed class NpcChatContext : PipelineContextBase
    {
        public ContextRequest Request { get; set; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
        public Result<NpcChatResult, RimMindError>? ChatResult { get; set; }
        public new CancellationToken Ct { get; set; }
        public bool IsStreaming { get; set; }
        public System.Action<string>? OnStreamChunk { get; set; }

        /// <summary>Convenience accessor: NpcId from Request.</summary>
        public string NpcId => Request?.NpcId ?? string.Empty;
        /// <summary>Convenience accessor: Message from Request.CurrentQuery.</summary>
        public string Message => Request?.CurrentQuery ?? string.Empty;
        /// <summary>Legacy alias for ChatResult's value. Used by Application-layer middleware.</summary>
        public NpcChatResult? Result
        {
            get => ChatResult.HasValue && ChatResult.Value.IsOk ? ChatResult.Value.Value : null;
            set
            {
                if (value != null)
                    ChatResult = Result<NpcChatResult, RimMindError>.Ok(value);
                else
                    ChatResult = null;
            }
        }
        public string? Context { get; set; }
        public int RetryCount { get; set; }

        public NpcChatContext() : base() { }

        public NpcChatContext(string npcId, string message, string? traceId = null, CancellationToken ct = default)
            : base(traceId, ct)
        {
            Ct = ct;
        }
    }
}
