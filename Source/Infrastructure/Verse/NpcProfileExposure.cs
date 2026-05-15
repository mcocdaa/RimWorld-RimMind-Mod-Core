using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Npc;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public static class NpcProfileExposure
    {
        public static void ExposeData(this NpcProfile profile)
        {
            string npcId = profile.NpcId ?? "";
            int pawnId = profile.PawnId;
            string name = profile.Name ?? "";
            string shortName = profile.ShortName ?? "";
            string displayName = profile.DisplayName ?? "";
            string backstory = profile.Backstory ?? "";
            string characterDescription = profile.CharacterDescription ?? "";
            string systemPrompt = profile.SystemPrompt ?? "";

            Scribe_Values.Look(ref npcId, "npcId");
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref shortName, "shortName");
            Scribe_Values.Look(ref displayName, "displayName");
            Scribe_Values.Look(ref backstory, "backstory");
            Scribe_Values.Look(ref characterDescription, "characterDescription");
            Scribe_Values.Look(ref systemPrompt, "systemPrompt");

            profile.NpcId = npcId ?? string.Empty;
            profile.PawnId = pawnId;
            profile.Name = name ?? string.Empty;
            profile.ShortName = shortName ?? string.Empty;
            profile.DisplayName = displayName ?? string.Empty;
            profile.Backstory = backstory ?? string.Empty;
            profile.CharacterDescription = characterDescription ?? string.Empty;
            profile.SystemPrompt = systemPrompt ?? string.Empty;
        }
    }
}
