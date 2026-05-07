using System.Collections.Generic;

namespace RimMind.Core.Client
{
    public class StructuredTool
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Parameters { get; set; }
        public string? ToolChoice { get; set; }
    }

    public class StructuredToolCall
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Arguments { get; set; } = "";
    }
}
