using System;

namespace RimMind.Contracts.Context
{
    public class HistoryEntry : Verse.IExposable
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

        public void ExposeData()
        {
            global::Verse.Scribe_Values.Look(ref Role, "role");
            global::Verse.Scribe_Values.Look(ref Content, "content");
            global::Verse.Scribe_Values.Look(ref Tick, "tick");
            global::Verse.Scribe_Values.Look(ref Scenario, "scenario", null!);
        }
    }
}
