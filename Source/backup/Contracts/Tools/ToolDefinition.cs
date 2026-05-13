namespace RimMind.Contracts.Tools
{
    public sealed record ToolDefinition
    {
        public string Id { get; init; } = "";
        public string Description { get; init; } = "";
        public string ParametersSchema { get; init; } = "{}";
        public string Category { get; init; } = "general";
    }
}
