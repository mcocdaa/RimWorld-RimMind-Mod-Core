using Verse;

namespace RimMind.Core.Context
{
    public class HistoryEntry : IExposable
    {
        public string Role;
        public string Content;
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

        public void ExposeData()
        {
            Scribe_Values.Look(ref Role, "role");
            Scribe_Values.Look(ref Content, "content");
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref Scenario, "scenario");
        }
    }
}
