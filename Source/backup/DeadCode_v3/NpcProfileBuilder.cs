using RimMind.Application.Common.Models.Npc;
using Verse;

namespace RimMind.Presentation.Agent
{
    public static class NpcProfileBuilder
    {
        public static NpcProfile BuildPawnNpc(Pawn pawn)
        {
            if (pawn == null) return new NpcProfile();
            string npcId = $"NPC-{pawn.thingIDNumber}";
            var profile = new NpcProfile(npcId, pawn.thingIDNumber, pawn.Name?.ToStringFull ?? pawn.Label ?? "Unknown");
            profile.ShortName = pawn.Name?.ToStringShort ?? pawn.Label ?? "Unknown";
            profile.Backstory = pawn.story?.Adulthood?.title ?? "";
            return profile;
        }
    }
}
