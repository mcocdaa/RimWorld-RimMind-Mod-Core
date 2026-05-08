using System.Collections.Generic;
using Verse;

namespace RimMind.Core.Agent
{
    public class AgentIdentity : IExposable
    {
        public string NpcId = "";
        public int PawnId;
        public string DisplayName = "";
        public List<string> Motivations = new List<string>();
        public List<string> PersonalityTraits = new List<string>();
        public List<string> CoreValues = new List<string>();

        public AgentIdentity() { }

        public AgentIdentity(string npcId, int pawnId, string displayName)
        {
            NpcId = npcId;
            PawnId = pawnId;
            DisplayName = displayName;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref NpcId, "npcId", "");
            Scribe_Values.Look(ref PawnId, "pawnId", 0);
            Scribe_Values.Look(ref DisplayName, "displayName", "");
        }

        public override string ToString() => $"AgentIdentity({NpcId}, PawnId={PawnId}, {DisplayName})";
    }
}
