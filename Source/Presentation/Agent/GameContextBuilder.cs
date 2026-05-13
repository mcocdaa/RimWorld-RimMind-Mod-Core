using System.Collections.Generic;
using System.Text;
using RimMind.Application.Common.Models.Context;
using RimMind.Presentation.Settings;
using Verse;

namespace RimMind.Presentation.Agent
{
    public static class GameContextBuilder
    {
        public static string BuildMapContext(Map map, bool brief = false)
        {
            if (map == null) return "";
            var sb = new StringBuilder();
            var s = RimMindCoreMod.Settings?.Context;
            if (s == null) return "";

            if (s.IncludeGameTime)
                sb.AppendLine($"[Game Time] Day {GenDate.DayOfQuadrum}, {GenDate.Quadrum}, Year {GenDate.Year}, Hour {GenDate.HourInteger}");

            if (s.IncludeSeason)
                sb.AppendLine($"[Season] {GenDate.Season}");

            if (s.IncludeWeather)
                sb.AppendLine($"[Weather] {map.weatherManager.CurWeatherLabel}");

            if (s.IncludeColonistCount)
                sb.AppendLine($"[Colonists] {map.mapPawns.FreeColonistsCount}");

            if (s.IncludeWealth)
                sb.AppendLine($"[Wealth] {map.wealthWatcher.WealthTotal:F0}");

            if (s.IncludeFood)
            {
                float food = map.resourceCounter.GetCountIn(ThingDefOf.MealSimple) +
                             map.resourceCounter.GetCountIn(ThingDefOf.MealFine) +
                             map.resourceCounter.GetCountIn(ThingDefOf.MealLavish);
                sb.AppendLine($"[Food] {food} meals");
            }

            if (s.IncludeThreats)
            {
                int threats = map.mapPawns.AllPawnsSpawned.Count(p => p.HostileTo(Faction.OfPlayer));
                sb.AppendLine($"[Threats] {threats} hostiles");
            }

            return sb.ToString();
        }

        public static string BuildPawnContext(Pawn pawn, ContextSettings? settings = null)
        {
            if (pawn == null) return "";
            var s = settings ?? RimMindCoreMod.Settings?.Context;
            if (s == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            var sb = new StringBuilder();

            if (s.IncludeRace) sb.AppendLine($"[Race] {data.RaceLabel}");
            if (s.IncludeAge) sb.AppendLine($"[Age] {data.Age}");
            if (s.IncludeGender) sb.AppendLine($"[Gender] {data.GenderLabel}");
            if (s.IncludeBackstory) sb.AppendLine($"[Backstory] {data.ChildhoodTitle}; {data.AdulthoodTitle}");
            if (s.IncludeTraits && data.Traits.Count > 0) sb.AppendLine($"[Traits] {string.Join(", ", data.Traits)}");
            if (s.IncludeSkills && data.Skills.Count > 0)
            {
                var skillStrs = new List<string>();
                foreach (var kv in data.Skills)
                    if (kv.Value >= s.MinSkillLevel)
                        skillStrs.Add($"{kv.Key}:{kv.Value}");
                if (skillStrs.Count > 0) sb.AppendLine($"[Skills] {string.Join(", ", skillStrs)}");
            }
            if (s.IncludeMood) sb.AppendLine($"[Mood] {data.MoodPercent:P0}");
            if (s.IncludeHealth && data.Hediffs.Count > 0)
            {
                var visible = new List<string>();
                foreach (var h in data.Hediffs)
                    if (h.Visible) visible.Add(h.HediffLabel);
                if (visible.Count > 0) sb.AppendLine($"[Health] {string.Join(", ", visible)}");
            }

            return sb.ToString();
        }
    }
}
