using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Npc;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public static class NpcProfileExposure
    {
        public static void ExposeData(this NpcProfile profile)
        {
            Scribe_Values.Look(ref profile.NpcId, "npcId");
            Scribe_Values.Look(ref profile.PawnId, "pawnId");
            Scribe_Values.Look(ref profile.Name, "name");
            Scribe_Values.Look(ref profile.ShortName, "shortName");
            Scribe_Values.Look(ref profile.DisplayName, "displayName");
            Scribe_Values.Look(ref profile.Backstory, "backstory");
            Scribe_Values.Look(ref profile.CharacterDescription, "characterDescription");
            Scribe_Values.Look(ref profile.SystemPrompt, "systemPrompt");
        }
    }
}
