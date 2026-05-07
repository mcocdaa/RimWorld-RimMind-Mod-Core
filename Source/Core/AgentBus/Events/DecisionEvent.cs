using RimMind.Contracts;

namespace RimMind.Kernel.Bus
{
    public class DecisionEvent : AgentBusEvent
    {
        public string DecisionType = "";
        public string Reason = "";
        public string Action = "";

        public DecisionEvent(string npcId, int pawnId, string decisionType, string reason, string action)
        {
            NpcId = npcId;
            PawnId = pawnId;
            DecisionType = decisionType;
            Reason = reason;
            Action = action;
            EventType = AgentBusEventType.Decision;
        }
    }
}
