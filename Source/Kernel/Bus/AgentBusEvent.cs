namespace RimMind.Kernel.Bus
{
    [global::System.Obsolete("Use RimMind.Contracts.AgentBusEvent instead. This alias is kept for backward compatibility.")]
    public class AgentBusEvent : RimMind.Contracts.AgentBusEvent
    {
        public AgentBusEvent() { }
        public AgentBusEvent(string npcId, int pawnId, RimMind.Contracts.AgentBusEventType eventType, int timestamp = 0)
            : base(npcId, pawnId, eventType, timestamp) { }
    }

    [global::System.Obsolete("Use RimMind.Contracts.AgentBusEventType instead. This alias is kept for backward compatibility.")]
    public enum AgentBusEventType
    {
        Perception = RimMind.Contracts.AgentBusEventType.Perception,
        Decision = RimMind.Contracts.AgentBusEventType.Decision,
        Goal = RimMind.Contracts.AgentBusEventType.Goal,
        Action = RimMind.Contracts.AgentBusEventType.Action,
        Lifecycle = RimMind.Contracts.AgentBusEventType.Lifecycle,
    }
}
