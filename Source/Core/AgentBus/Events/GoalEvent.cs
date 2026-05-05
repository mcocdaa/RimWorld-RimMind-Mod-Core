namespace RimMind.Core.AgentBus
{
    public class GoalEvent : AgentBusEvent
    {
        public string GoalDescription = "";
        public string Status = "";
        public string Category = "";

        public GoalEvent(string npcId, int pawnId, string goalDescription, string status, string category)
        {
            NpcId = npcId;
            PawnId = pawnId;
            GoalDescription = goalDescription;
            Status = status;
            Category = category;
            EventType = AgentBusEventType.Goal;
        }
    }
}
