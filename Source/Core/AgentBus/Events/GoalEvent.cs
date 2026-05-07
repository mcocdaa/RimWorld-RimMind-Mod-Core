namespace RimMind.Kernel.Bus
{
    public class GoalEvent : Contracts.AgentBusEvent
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
            EventType = Contracts.AgentBusEventType.Goal;
        }
    }
}
