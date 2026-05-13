namespace RimMind.Application.Common.Models.UI
{
    public class RequestEntry
    {
        public string RequestId { get; set; } = "";
        public string NpcId { get; set; } = "";
        public string ModId { get; set; } = "";
        public string State { get; set; } = "Queued";
        public int EnqueuedAtTick { get; set; }
        public int AttemptCount { get; set; }
        public string? TraceId { get; set; }
    }
}
