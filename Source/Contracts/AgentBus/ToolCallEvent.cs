namespace RimMind.Contracts.AgentBus
{
    public class ToolCallEvent : AgentBusEvent
    {
        public string ToolId { get; set; } = "";
        public string ToolCallId { get; set; } = "";
        public string TraceId { get; set; } = "";
        public string ArgumentsJson { get; set; } = "{}";

        public ToolCallEvent() { }

        public ToolCallEvent(string npcId, int pawnId, string toolId, string toolCallId, string traceId, string argumentsJson = "{}")
            : base(npcId, pawnId, AgentBusEventType.ToolCall)
        {
            ToolId = toolId;
            ToolCallId = toolCallId;
            TraceId = traceId;
            ArgumentsJson = argumentsJson;
        }
    }
}
