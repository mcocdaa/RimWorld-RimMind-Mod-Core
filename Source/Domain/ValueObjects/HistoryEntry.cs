namespace RimMind.Domain.ValueObjects
{
    public class HistoryEntry
    {
        public string Role = "";
        public string Content = "";
        public int Tick;
        public string? Scenario;

        public HistoryEntry() { }

        public HistoryEntry(string role, string content, int tick, string? scenario = null)
        {
            Role = role;
            Content = content;
            Tick = tick;
            Scenario = scenario;
        }
    }
}
