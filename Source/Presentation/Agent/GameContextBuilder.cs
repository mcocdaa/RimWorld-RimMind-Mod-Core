using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Prompt;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Prompt;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Agent;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class GameContextBuilder : IGameContextBuilder
    {
        public static string BuildMapContext(Map map, bool brief = false)
        {
            var entries = BuildMapContextEntries(map, brief);
            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Content))
                    sb.AppendLine(entry.Content);
            }
            return sb.ToString().TrimEnd();
        }

        public static List<ContextEntry> BuildMapContextEntries(Map map, bool brief = false)
        {
            var entries = new List<ContextEntry>();
            if (map == null) return entries;

            var ctx = RimMindServiceLocator.Get<ISettingsProvider>()?.Context;

            entries.Add(new ContextEntry("RimMind.Presentation.Prompt.MapStatusHeader".Translate()));

            if (ctx.IncludeGameTime)
            {
                long ticks = Find.TickManager.TicksAbs;
                Vector2 longLat = Find.WorldGrid.LongLatOf(map.Tile);
                int hour = GenDate.HourOfDay(ticks, longLat.x);
                string dateStr = GenDate.DateFullStringAt(ticks, longLat);
                int day = (int)(ticks / 60000L);
                entries.Add(new ContextEntry(
                    "RimMind.Presentation.Prompt.TimeFormat".Translate(dateStr, $"{hour:D2}"))
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "time",
                        ["hour"] = hour.ToString(),
                        ["day"] = day.ToString()
                    }
                });
            }

            if (ctx.IncludeColonistCount)
            {
                var colonists = map.mapPawns.FreeColonistsSpawned;
                int count = colonists.Count;
                string content;
                if (ctx.IncludeColonistNames && colonists.Count > 0)
                {
                    var names = colonists.Select(p => p.Name.ToStringShort);
                    string nameList = string.Join(", ", names);
                    content = "RimMind.Presentation.Prompt.ColonistCount".Translate(count, nameList);
                }
                else
                {
                    content = "RimMind.Presentation.Prompt.ColonistCountBrief".Translate(count);
                }
                entries.Add(new ContextEntry(content)
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "colonistCount",
                        ["count"] = count.ToString()
                    }
                });
            }

            var otherSb = new StringBuilder();

            if (ctx.IncludeColonistNames)
            {
                var prisoners = map.mapPawns.PrisonersOfColonySpawned;
                if (prisoners.Count > 0)
                {
                    var names = prisoners.Select(p => p.Name.ToStringShort);
                    string nameList = string.Join(", ", names);
                    otherSb.AppendLine("RimMind.Presentation.Prompt.PrisonerCount".Translate(prisoners.Count, nameList));
                }
            }

            if (ctx.IncludeWealth)
            {
                float wealth = map.wealthWatcher.WealthTotal;
                string threat = ThreatLabel(wealth);
                otherSb.AppendLine("RimMind.Presentation.Prompt.WealthWithThreat".Translate($"{wealth:F0}", threat));
            }

            if (ctx.IncludeThreats)
            {
                float wealth = map.wealthWatcher.WealthTotal;
                string threat = ThreatLabel(wealth);
                otherSb.AppendLine("RimMind.Presentation.Prompt.ThreatLevel".Translate(threat));
            }

            if (ctx.IncludeFood)
            {
                float foodNutrition = 0f;
                for (int i = 0; i < DefDatabase<ThingDef>.AllDefsListForReading.Count; i++)
                {
                    var def = DefDatabase<ThingDef>.AllDefsListForReading[i];
                    if (def.IsNutritionGivingIngestible && def.ingestible != null
                        && def.ingestible.HumanEdible && !def.IsCorpse)
                    {
                        foodNutrition += map.resourceCounter.GetCount(def) * def.ingestible.CachedNutrition;
                    }
                }
                otherSb.AppendLine("RimMind.Presentation.Prompt.FoodStorage".Translate($"{foodNutrition:F0}"));
            }

            if (ctx.IncludeSeason)
                otherSb.Append("RimMind.Presentation.Prompt.Season".Translate(GenLocalDate.Season(map).Label()));
            if (ctx.IncludeWeather)
                otherSb.AppendLine("RimMind.Presentation.Prompt.Weather".Translate(map.weatherManager.curWeather.label));
            else if (ctx.IncludeSeason)
                otherSb.AppendLine();

            string otherContent = otherSb.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(otherContent))
                entries.Add(new ContextEntry(otherContent));

            return entries;
        }

        public static string BuildPawnContext(Pawn pawn)
        {
            if (pawn == null) return string.Empty;

            var data = PawnDataExtractor.Extract(pawn);
            var ctx = RimMindServiceLocator.Get<ISettingsProvider>()?.Context;
            var sb = new StringBuilder();
            sb.Append("RimMind.Presentation.Prompt.PawnStatusHeader".Translate(data.Name) + "  ");

            var basics = new List<string>();
            if (ctx.IncludeAge) basics.Add("RimMind.Presentation.Prompt.AgeFormat".Translate(data.Age));
            if (ctx.IncludeGender) basics.Add(data.GenderLabel);
            if (ctx.IncludeRace) basics.Add(data.RaceLabel);
            if (basics.Count > 0) sb.AppendLine(string.Join("  ", basics));
            else sb.AppendLine();

            if (ctx.IncludeGenes && data.NotableGenes.Count > 0)
                sb.AppendLine("RimMind.Presentation.Prompt.Genes".Translate(string.Join(", ", data.NotableGenes)));

            if (ctx.IncludeBackstory && (data.ChildhoodTitle != null || data.AdulthoodTitle != null))
            {
                var parts = new List<string>();
                if (data.ChildhoodTitle != null)
                    parts.Add("RimMind.Presentation.Prompt.Childhood".Translate(data.ChildhoodTitle));
                if (data.AdulthoodTitle != null)
                    parts.Add("RimMind.Presentation.Prompt.Adulthood".Translate(data.AdulthoodTitle));
                if (parts.Count > 0)
                    sb.AppendLine("RimMind.Presentation.Prompt.Backstory".Translate(string.Join("  ", parts)));
            }

            if (ctx.IncludeIdeology && data.IdeologyName != null)
                sb.AppendLine("RimMind.Presentation.Prompt.IdeologyFormat".Translate(data.IdeologyName, data.IdeologyMemes));

            if (ctx.IncludeMood && data.MoodString != null)
            {
                if (data.InMentalState)
                    sb.AppendLine("RimMind.Presentation.Prompt.MoodBreak".Translate(data.MoodString, data.MentalStateInspectLine));
                else if (data.Downed)
                    sb.AppendLine("RimMind.Presentation.Prompt.MoodDowned".Translate(data.MoodString));
                else
                    sb.AppendLine("RimMind.Presentation.Prompt.MoodPercent".Translate(data.MoodString, $"{data.MoodPercent:F0}"));
            }

            if (ctx.IncludeMoodThoughts && data.MoodThoughts.Count > 0)
            {
                var factors = data.MoodThoughts.Select(t => $"{t.Label}({t.Offset:+0;-0})");
                sb.AppendLine("RimMind.Presentation.Prompt.MoodFactors".Translate(string.Join(", ", factors)));
            }

            if (ctx.IncludeHealth && data.Hediffs.Count > 0)
            {
                var notable = new List<string>();
                foreach (var h in data.Hediffs)
                {
                    if (!h.IsBad || h.Severity < 0.05f || !h.Visible) continue;
                    string partLabel = h.PartLabel ?? "RimMind.Presentation.Prompt.FullBody".Translate();
                    notable.Add($"{partLabel}: {h.HediffLabel}");
                }
                if (notable.Count > 0)
                    sb.AppendLine("RimMind.Presentation.Prompt.HealthIssues".Translate(string.Join(", ", notable.Take(8))));
            }

            if (ctx.IncludeCapacities && data.Capacities.Count > 0)
            {
                var low = data.Capacities.Select(c => $"{c.Label}{c.Level * 100f:F0}%");
                sb.AppendLine("RimMind.Presentation.Prompt.Capacities".Translate(string.Join(", ", low)));
            }

            if (ctx.IncludeSkills && data.Skills.Count > 0)
            {
                var skills = data.Skills
                    .Where(s => s.Value >= ctx.MinSkillLevel)
                    .Select(s => $"{s.Key}({s.Value})")
                    .ToList();
                if (skills.Count > 0)
                    sb.AppendLine("RimMind.Presentation.Prompt.Skills".Translate(string.Join("  ", skills)));
            }

            if (ctx.IncludeCurrentJob)
            {
                string jobLabel = data.CurrentJobReport
                    ?? data.CurrentJobDefLabel
                    ?? "RimMind.Presentation.Prompt.None".Translate();
                sb.AppendLine("RimMind.Presentation.Prompt.CurrentJob".Translate(jobLabel));
            }

            if (ctx.IncludeWorkPriorities && data.WorkPriorities.Count > 0)
            {
                sb.AppendLine("RimMind.Presentation.Prompt.WorkPriorities".Translate(
                    string.Join("  ", data.WorkPriorities.Select(e => $"{e.Label}({e.Priority})"))));
            }

            if (ctx.IncludeTraits && !string.IsNullOrEmpty(data.TraitLabels))
                sb.AppendLine("RimMind.Presentation.Prompt.Traits".Translate(data.TraitLabels));

            if (ctx.IncludeEquipment)
            {
                var parts = new List<string>();
                if (data.WeaponLabel != null)
                    parts.Add("RimMind.Presentation.Prompt.Weapon".Translate(data.WeaponLabel));
                if (data.ApparelLabels.Count > 0)
                    parts.Add("RimMind.Presentation.Prompt.Apparel".Translate(string.Join(", ", data.ApparelLabels)));
                if (parts.Count > 0)
                    sb.AppendLine(string.Join("  ", parts));
            }

            if (ctx.IncludeInventory && data.InventoryItems.Count > 0)
            {
                var itemStrs = data.InventoryItems.OrderByDescending(kv => kv.Value)
                    .Take(8)
                    .Select(kv =>
                    {
                        var def = DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key);
                        string label = def?.LabelCap ?? kv.Key;
                        return kv.Value > 1 ? $"{label}×{kv.Value}" : label;
                    });
                sb.AppendLine("RimMind.Presentation.Prompt.Inventory".Translate(string.Join(", ", itemStrs)));
            }

            if (ctx.IncludeLocation && data.HasMap)
            {
                int temp = Mathf.RoundToInt(data.Temperature);
                sb.AppendLine("RimMind.Presentation.Prompt.Location".Translate(data.RoomLabel, $"{temp}"));
            }

            if (ctx.IncludeRelations && data.Relations.Count > 0)
            {
                var relParts = data.Relations.Select(r => $"{r.RelationLabel}({r.OtherName})");
                sb.AppendLine("RimMind.Presentation.Prompt.Relations".Translate(string.Join(", ", relParts)));
            }

            if (ctx.IncludeCombatStatus)
            {
                if (data.InCombat)
                {
                    string targetLabel = data.EnemyTargetLabel ?? "RimMind.Presentation.Prompt.Unknown".Translate();
                    sb.AppendLine("RimMind.Presentation.Prompt.InCombat".Translate(targetLabel));
                }
                if (data.Drafted)
                    sb.AppendLine("RimMind.Presentation.Prompt.Drafted".Translate());
            }

            if (ctx.IncludeSurroundings && pawn.Map != null)
            {
                string surroundings = BuildSurroundings(pawn);
                if (!string.IsNullOrEmpty(surroundings))
                    sb.AppendLine("RimMind.Presentation.Prompt.Surroundings".Translate(surroundings));
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildSurroundings(Pawn pawn, int? radius = null, int? maxItems = null)
        {
            int r = radius ?? (RimMindServiceLocator.Get<ISettingsProvider>()?.Context?.EnvironmentScanRadius ?? 5);
            int m = maxItems ?? (RimMindServiceLocator.Get<ISettingsProvider>()?.Context?.EnvironmentMaxItems ?? 8);
            var map = pawn.Map;
            var buildings = new List<string>();
            var items = new Dictionary<string, int>();
            var animals = new List<string>();

            foreach (var c in GenRadial.RadialCellsAround(pawn.Position, r, true))
            {
                if (!c.InBounds(map)) continue;
                var room = pawn.GetRoom();
                if (room != null && !room.PsychologicallyOutdoors)
                {
                    var cRoom = c.GetRoom(map);
                    if (cRoom != room) continue;
                }

                var things = c.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    var thing = things[i];
                    if (thing.DestroyedOrNull() || thing == pawn) continue;

                    if (thing is Pawn otherPawn)
                    {
                        if (otherPawn.RaceProps.Animal && !otherPawn.Dead)
                            animals.Add(otherPawn.LabelShort ?? otherPawn.def.label);
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Building)
                    {
                        if (buildings.Count < 5)
                            buildings.Add(thing.def.LabelCap);
                    }
                    else if (thing.def.category == ThingCategory.Item)
                    {
                        string key = thing.def.defName;
                        if (!items.ContainsKey(key))
                            items[key] = 0;
                        items[key] += thing.stackCount;
                    }
                }
            }

            var parts = new List<string>();
            if (buildings.Count > 0)
                parts.Add("RimMind.Presentation.Prompt.SurroundingsBuildings".Translate(string.Join(", ", buildings.Distinct().Take(5))));
            if (items.Count > 0)
            {
                var itemStrs = items.OrderByDescending(kv => kv.Value)
                    .Take(m)
                    .Select(kv => $"{DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key)?.LabelCap ?? kv.Key}×{kv.Value}");
                parts.Add("RimMind.Presentation.Prompt.SurroundingsItems".Translate(string.Join(", ", itemStrs)));
            }
            if (animals.Count > 0)
                parts.Add("RimMind.Presentation.Prompt.SurroundingsAnimals".Translate(string.Join(", ", animals.Distinct().Take(4))));

            return parts.Count > 0 ? string.Join("  ", parts) : string.Empty;
        }

        private static string ThreatLabel(float wealth)
        {
            float high = RimMindServiceLocator.Get<ISettingsProvider>()?.Context?.ThreatThresholdHigh ?? 200000f;
            float medium = RimMindServiceLocator.Get<ISettingsProvider>()?.Context?.ThreatThresholdMedium ?? 100000f;
            float low = RimMindServiceLocator.Get<ISettingsProvider>()?.Context?.ThreatThresholdLow ?? 50000f;

            float threatScale = 1f;
            try { threatScale = Find.Storyteller?.difficulty?.threatScale ?? 1f; } catch { }
            if (threatScale <= 0f) threatScale = 1f;

            string tier = ThreatClassifier.ClassifyThreatTier(wealth, high, medium, low, threatScale);
            return tier switch
            {
                "Extreme" => "RimMind.Presentation.Prompt.Threat.Extreme".Translate(),
                "High"    => "RimMind.Presentation.Prompt.Threat.High".Translate(),
                "Medium"  => "RimMind.Presentation.Prompt.Threat.Medium".Translate(),
                _         => "RimMind.Presentation.Prompt.Threat.Low".Translate()
            };
        }

        public string CollectBasicGameState(string npcId)
        {
            var sb = new StringBuilder();
            var pawnObj = RimMindServiceLocator.Get<INpcManager>()?.FindPawnByNpcId(npcId);
            var pawn = pawnObj as Pawn;

            if (pawn != null)
            {
                if (pawn.Map != null)
                    sb.AppendLine(BuildMapContext(pawn.Map));
                sb.AppendLine(BuildPawnContext(pawn));
            }
            else
            {
                var map = Find.CurrentMap;
                if (map != null)
                    sb.AppendLine(BuildMapContext(map));
            }

            return sb.ToString().TrimEnd();
        }

        public static PromptSection BuildMapContextSection(Map map, bool brief = false)
        {
            var section = new PromptSection("map_context", BuildMapContext(map, brief), PromptSection.PriorityKeyState);
            section.Compress = _ => BuildMapContext(map, brief: true);
            return section;
        }

        public static PromptSection BuildPawnContextSection(Pawn pawn)
        {
            var section = new PromptSection("pawn_context", BuildPawnContext(pawn), PromptSection.PriorityKeyState);
            section.Compress = _ => BuildCompactPawnContext(pawn);
            return section;
        }

        public static PromptSection BuildCompactPawnContextSection(Pawn pawn)
        {
            return new PromptSection("pawn_compact", BuildCompactPawnContext(pawn), PromptSection.PriorityKeyState);
        }

        public static string BuildCompactPawnContext(Pawn pawn)
        {
            if (pawn == null) return string.Empty;

            var data = PawnDataExtractor.Extract(pawn);
            var sb = new StringBuilder();
            sb.Append(data.Name + "  ");

            var basics = new List<string>();
            basics.Add("RimMind.Presentation.Prompt.AgeFormat".Translate(data.Age));
            basics.Add(data.GenderLabel);
            basics.Add(data.RaceLabel);
            sb.AppendLine(string.Join("  ", basics));

            if (data.MoodString != null)
            {
                string moodLabel = data.InMentalState ? "RimMind.Presentation.Prompt.CompactMentalBreak".Translate()
                    : data.Downed ? "RimMind.Presentation.Prompt.CompactDowned".Translate()
                    : $"{data.MoodPercent:F0}%";
                sb.AppendLine("RimMind.Presentation.Prompt.CompactMood".Translate(moodLabel));
            }

            if (data.Hediffs.Count > 0)
            {
                var notable = new List<string>();
                foreach (var h in data.Hediffs)
                {
                    if (!h.IsBad || h.Severity < 0.05f || !h.Visible) continue;
                    string partLabel = h.PartLabel ?? "RimMind.Presentation.Prompt.FullBody".Translate();
                    notable.Add($"{partLabel}:{h.HediffLabel}");
                    if (notable.Count >= 3) break;
                }
                if (notable.Count > 0)
                    sb.AppendLine("RimMind.Presentation.Prompt.CompactHealth".Translate(string.Join(", ", notable)));
            }

            string jobLabel = data.CurrentJobReport
                ?? data.CurrentJobDefLabel
                ?? "RimMind.Presentation.Prompt.None".Translate();
            sb.AppendLine("RimMind.Presentation.Prompt.CompactJob".Translate(jobLabel));

            if (data.HasMap)
            {
                int temp = Mathf.RoundToInt(data.Temperature);
                sb.AppendLine("RimMind.Presentation.Prompt.CompactLocation".Translate(data.RoomLabel, $"{temp}"));
            }

            if (data.WeaponLabel != null)
                sb.AppendLine("RimMind.Presentation.Prompt.CompactWeapon".Translate(data.WeaponLabel));

            if (data.Drafted)
                sb.AppendLine("RimMind.Presentation.Prompt.Drafted".Translate());
            if (data.EnemyTargetLabel != null)
                sb.AppendLine("RimMind.Presentation.Prompt.InCombat".Translate(data.EnemyTargetLabel));

            return sb.ToString().TrimEnd();
        }

        public static string ExtractPawnBaseInfo(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            var parts = new List<string>();
            parts.Add(data.Name);
            parts.Add($"{data.Age}yo");
            parts.Add(data.GenderLabel);
            parts.Add(data.RaceLabel);
            if (data.ChildhoodTitle != null)
                parts.Add(data.ChildhoodTitle);
            if (data.AdulthoodTitle != null)
                parts.Add(data.AdulthoodTitle);
            if (data.TraitLabels.Length > 0)
                parts.Add($"Traits: {string.Join(", ", data.TraitLabels)}");
            return string.Join(" | ", parts);
        }

        public static string ExtractFixedRelations(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (data.Relations.Count == 0) return "";
            return string.Join(", ", data.Relations.Select(r => $"{r.RelationLabel}({r.OtherName})"));
        }

        public static string ExtractIdeology(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (data.IdeologyName == null) return "";
            return $"{data.IdeologyName}{data.IdeologyMemes}";
        }

        public static string ExtractSkillsSummary(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (data.Skills.Count == 0) return "";
            var top = data.Skills
                .OrderByDescending(s => s.Value)
                .Take(5)
                .Select(s => $"{s.Key}({s.Value})");
            return string.Join("  ", top);
        }

        public static string ExtractCurrentArea(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (!data.HasMap) return "";
            int temp = Mathf.RoundToInt(data.Temperature);
            return $"{data.RoomLabel}, {temp}°C";
        }

        public static string ExtractWeather(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            return data.WeatherLabel ?? "";
        }

        public static string ExtractTimeOfDay(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            return data.TimeString ?? "";
        }

        public static string ExtractNearbyPawns(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            return !string.IsNullOrEmpty(data.NearbyPawnNames) ? data.NearbyPawnNames : "";
        }

        public static string ExtractSeason(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            return data.SeasonLabel ?? "";
        }

        public static string ExtractColonyStatus(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (!data.HasMap) return "";
            return "RimMind.Presentation.Prompt.Colony.Status".Translate(data.ColonistCount, $"{data.ColonyWealth:F0}", data.ThreatCount);
        }

        public static string ExtractHealth(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            var notable = data.Hediffs
                .Where(h => h.Visible)
                .Select(h => h.HediffLabel)
                .Take(5)
                .ToList();
            return notable.Count > 0 ? string.Join(", ", notable) : "RimMind.Presentation.Prompt.Health.Healthy".Translate();
        }

        public static string ExtractMood(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (data.MoodString == null) return "";
            if (data.InMentalState)
                return "RimMind.Presentation.Prompt.Mood.MentalBreak".Translate(data.MentalStateInspectLine ?? "");
            return $"{data.MoodPercent:F0}%";
        }

        public static string ExtractCurrentJob(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            return data.CurrentJobReport ?? data.CurrentJobDefLabel ?? "RimMind.Presentation.Prompt.Job.Idle".Translate();
        }

        public static string ExtractCombatStatus(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            var parts = new List<string>();
            if (data.Drafted) parts.Add("RimMind.Presentation.Prompt.Combat.Drafted".Translate());
            if (data.EnemyTargetLabel != null)
                parts.Add("RimMind.Presentation.Prompt.Combat.Fighting".Translate(data.EnemyTargetLabel));
            return parts.Count > 0 ? string.Join(" | ", parts) : "RimMind.Presentation.Prompt.Combat.NotInCombat".Translate();
        }

        public static string ExtractTargetInfo(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (data.EnemyTargetLabel == null) return "";
            string label = data.EnemyTargetHpPercent.HasValue
                ? $"{data.EnemyTargetLabel} (HP:{data.EnemyTargetHpPercent.Value:F0}%)"
                : data.EnemyTargetLabel;
            return "RimMind.Presentation.Prompt.Target.Info".Translate(label);
        }
    }
}
