namespace RimMind.Domain.Events
{
    public class ChoiceLetterDecidedEvent : AgentBusEvent
    {
        public string LetterId { get; set; } = "";
        public string ChosenOption { get; set; } = "";

        public ChoiceLetterDecidedEvent() { }

        public ChoiceLetterDecidedEvent(string npcId, int pawnId, string letterId, string chosenOption)
            : base(npcId, pawnId, AgentBusEventType.Action)
        {
            LetterId = letterId;
            ChosenOption = chosenOption;
        }
    }
}
