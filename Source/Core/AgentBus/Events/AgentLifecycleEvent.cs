using RimMind.Contracts;

namespace RimMind.Kernel.Bus
{
    public class AgentLifecycleEvent : AgentBusEvent
    {
        public string PreviousState = "";
        public string NewState = "";

        public AgentLifecycleEvent(string npcId, int pawnId, string previousState, string newState)
        {
            NpcId = npcId;
            PawnId = pawnId;
            PreviousState = previousState;
            NewState = newState;
            EventType = AgentBusEventType.Lifecycle;
        }
    }
}
