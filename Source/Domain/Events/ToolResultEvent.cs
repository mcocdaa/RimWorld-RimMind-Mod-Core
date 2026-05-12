namespace RimMind.Domain.Events
{
    public class ToolResultEvent : AgentBusEvent
    {
        public string ToolCallId { get; set; } = "";
        public string ToolId { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsError { get; set; }

        public ToolResultEvent() { }

        public ToolResultEvent(string npcId, int pawnId, string toolCallId, string toolId, string content, bool isError)
            : base(npcId, pawnId, AgentBusEventType.ToolResult)
        {
            ToolCallId = toolCallId;
            ToolId = toolId;
            Content = content;
            IsError = isError;
        }
    }
}
