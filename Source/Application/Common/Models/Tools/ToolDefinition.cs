using System.Collections.Generic;

namespace RimMind.Application.Common.Models.Tools
{
    public sealed record ToolDefinition
    {
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string? ParametersJson { get; init; }
        public IReadOnlyList<string>? RequiredParams { get; init; }
    }
}
