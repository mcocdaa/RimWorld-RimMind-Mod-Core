namespace RimMind.Domain.Events
{
    public enum AgentBusEventType
    {
        Perception,
        Decision,
        Goal,
        Action,
        Lifecycle,
        ModeChange,
        InnerVoice,
        Reflection,
        ScheduleUpdate,
        MoodThreshold,
        NeedCritical,
        MentalStateWarning,
        InformationDiffusion,
        SocialEventProposed,
        TraitEvolution,
        Dream,
        DecisionFailed,
        WorkflowPhaseChange
    }

    public class AgentBusEvent
    {
        public string NpcId = "";
        public int PawnId;
        public AgentBusEventType EventType;
        public int Timestamp;

        public AgentBusEvent() { }

        public AgentBusEvent(string npcId, int pawnId, AgentBusEventType eventType, int timestamp = 0)
        {
            NpcId = npcId;
            PawnId = pawnId;
            EventType = eventType;
            Timestamp = timestamp;
        }
    }
}
