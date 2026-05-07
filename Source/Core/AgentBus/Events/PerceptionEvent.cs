namespace RimMind.Kernel.Bus
{
    public class PerceptionEvent : Contracts.AgentBusEvent
    {
        public string PerceptionType = "";
        public string Content = "";
        public float Importance;
        public new int Timestamp;

        public PerceptionEvent(string npcId, int pawnId, string perceptionType, string content, float importance = 0f)
        {
            NpcId = npcId;
            PawnId = pawnId;
            PerceptionType = perceptionType;
            Content = content;
            Importance = importance;
            Timestamp = Verse.Find.TickManager?.TicksGame ?? 0;
            EventType = Contracts.AgentBusEventType.Perception;
        }
    }
}
