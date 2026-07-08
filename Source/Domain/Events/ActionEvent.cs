namespace RimMind.Domain.Events
{
    public class ActionEvent : AgentBusEvent
    {
        public string ActionName = "";
        public bool Success;
        public string ResultReason = "";
        public string EventId = "";

        public ActionEvent(string npcId, int pawnId, string actionName, bool success, string resultReason, string eventId)
        {
            NpcId = npcId;
            PawnId = pawnId;
            ActionName = actionName;
            Success = success;
            ResultReason = resultReason;
            EventId = eventId;
            BusEventType = AgentBusEventType.Action;
        }
    }
}
