using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Npc
{
    internal sealed class NpcChatContext : PipelineContextBase
    {
        public string NpcId { get; }
        public string Message { get; }
        public string? Context { get; set; }
        public NpcChatResult? Result { get; set; }
        public int RetryCount { get; set; }

        public NpcChatContext(string npcId, string message, string? traceId = null, System.Threading.CancellationToken ct = default)
            : base(traceId, ct)
        {
            NpcId = npcId;
            Message = message;
        }
    }
}
