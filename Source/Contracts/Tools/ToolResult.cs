namespace RimMind.Contracts.Tools
{
    public sealed record ToolResult
    {
        public string ToolCallId { get; init; } = "";
        public string Content { get; init; } = "";
        public bool IsError { get; init; }
    }
}
