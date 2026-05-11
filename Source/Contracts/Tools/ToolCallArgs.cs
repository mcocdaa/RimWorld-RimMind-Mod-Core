namespace RimMind.Contracts.Tools
{
    public sealed record ToolCallArgs
    {
        public string ToolId { get; init; } = "";
        public string ToolCallId { get; init; } = "";
        public string ArgumentsJson { get; init; } = "{}";
        public int? PawnId { get; init; }
        public string? TraceId { get; init; }
    }
}
