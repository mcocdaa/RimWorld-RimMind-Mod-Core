using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Core.Context;
using RimMind.Core.Npc;
using RimMind.Core.Prompt;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimMind.Core.Internal
{
    /// <summary>
    /// 构建游戏状态上下文文本，供各模块组装 AI Prompt。
    /// 根据 RimMindCoreSettings.Context 过滤器选择性输出字段。
    /// 所有方法均在主线程调用。
    /// </summary>
    public static class GameContextBuilder
    {
        // ── 地图级上下文 ──────────────────────────────────────────────────────

        /// <summary>
        /// 构建地图状态摘要（约 100~300 token）。
        /// brief=true 时只输出关键字段，适合 AIAdvisor 子模块使用。
        /// </summary>
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

            var ctx = RimMindCoreMod.Settings.Context;

            entries.Add(new ContextEntry("RimMind.Core.Prompt.MapStatusHeader".Translate()));

            if (ctx.IncludeGameTime)
            {
                long ticks = Find.TickManager.TicksAbs;
                Vector2 longLat = Find.WorldGrid.LongLatOf(map.Tile);
                int hour = GenDate.HourOfDay(ticks, longLat.x);
                string dateStr = GenDate.DateFullStringAt(ticks, longLat);
                int day = (int)(ticks / 60000L);
                entries.Add(new ContextEntry(
                    "RimMind.Core.Prompt.TimeFormat".Translate(dateStr, $"{hour:D2}"))
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
                    content = "RimMind.Core.Prompt.ColonistCount".Translate(count, nameList);
                }
                else
                {
                    content = "RimMind.Core.Prompt.ColonistCountBrief".Translate(count);
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
                    otherSb.AppendLine("RimMind.Core.Prompt.PrisonerCount".Translate(prisoners.Count, nameList));
                }
            }

            if (ctx.IncludeWealth)
            {
                float wealth = map.wealthWatcher.WealthTotal;
                string threat = ThreatLabel(wealth);
                otherSb.AppendLine("RimMind.Core.Prompt.WealthWithThreat".Translate($"{wealth:F0}", threat));
            }

            if (ctx.IncludeThreats)
            {
                float wealth = map.wealthWatcher.WealthTotal;
                string threat = ThreatLabel(wealth);
                otherSb.AppendLine("RimMind.Core.Prompt.ThreatLevel".Translate(threat));
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
                otherSb.AppendLine("RimMind.Core.Prompt.FoodStorage".Translate($"{foodNutrition:F0}"));
            }

            if (ctx.IncludeSeason)
                otherSb.Append("RimMind.Core.Prompt.Season".Translate(GenLocalDate.Season(map).Label()));
            if (ctx.IncludeWeather)
                otherSb.AppendLine("RimMind.Core.Prompt.Weather".Translate(map.weatherManager.curWeather.label));
            else if (ctx.IncludeSeason)
                otherSb.AppendLine();

            string otherContent = otherSb.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(otherContent))
                entries.Add(new ContextEntry(otherContent));

            return entries;
        }

        // ── Pawn 级上下文 ─────────────────────────────────────────────────────

        /// <summary>
        /// 构建小人客观游戏状态（不含扩展 mod 注入内容）。
        /// 完整上下文请用 ContextEngine.BuildSnapshot。
        /// </summary>
        public static string BuildPawnContext(Pawn pawn)
        {
            if (pawn == null) return string.Empty;

            var data = PawnDataExtractor.Extract(pawn);
            var ctx = RimMindCoreMod.Settings.Context;
            var sb = new StringBuilder();
            sb.Append("RimMind.Core.Prompt.PawnStatusHeader".Translate(data.Name) + "  ");

            var basics = new List<string>();
            if (ctx.IncludeAge) basics.Add("RimMind.Core.Prompt.AgeFormat".Translate(data.Age));
            if (ctx.IncludeGender) basics.Add(data.GenderLabel);
            if (ctx.IncludeRace) basics.Add(data.RaceLabel);
            if (basics.Count > 0) sb.AppendLine(string.Join("  ", basics));
            else sb.AppendLine();

            if (ctx.IncludeGenes && data.NotableGenes.Count > 0)
                sb.AppendLine("RimMind.Core.Prompt.Genes".Translate(string.Join(", ", data.NotableGenes)));

            if (ctx.IncludeBackstory && (data.ChildhoodTitle != null || data.AdulthoodTitle != null))
            {
                var parts = new List<string>();
                if (data.ChildhoodTitle != null)
                    parts.Add("RimMind.Core.Prompt.Childhood".Translate(data.ChildhoodTitle));
                if (data.AdulthoodTitle != null)
                    parts.Add("RimMind.Core.Prompt.Adulthood".Translate(data.AdulthoodTitle));
                if (parts.Count > 0)
                    sb.AppendLine("RimMind.Core.Prompt.Backstory".Translate(string.Join("  ", parts)));
            }

            if (ctx.IncludeIdeology && data.IdeologyName != null)
                sb.AppendLine("RimMind.Core.Prompt.IdeologyFormat".Translate(data.IdeologyName, data.IdeologyMemes));

            if (ctx.IncludeMood && data.MoodString != null)
            {
                if (data.InMentalState)
                    sb.AppendLine("RimMind.Core.Prompt.MoodBreak".Translate(data.MoodString, data.MentalStateInspectLine));
                else if (data.Downed)
                    sb.AppendLine("RimMind.Core.Prompt.MoodDowned".Translate(data.MoodString));
                else
                    sb.AppendLine("RimMind.Core.Prompt.MoodPercent".Translate(data.MoodString, $"{data.MoodPercent:F0}"));
            }

            if (ctx.IncludeMoodThoughts && data.MoodThoughts.Count > 0)
            {
                var factors = data.MoodThoughts.Select(t => $"{t.Label}({t.Offset:+0;-0})");
                sb.AppendLine("RimMind.Core.Prompt.MoodFactors".Translate(string.Join(", ", factors)));
            }

            if (ctx.IncludeHealth && data.Hediffs.Count > 0)
            {
                var notable = new List<string>();
                foreach (var h in data.Hediffs)
                {
                    if (!h.IsBad || h.Severity < 0.05f || !h.Visible) continue;
                    string partLabel = h.PartLabel ?? "RimMind.Core.Prompt.FullBody".Translate();
                    notable.Add($"{partLabel}: {h.HediffLabel}");
                }
                if (notable.Count > 0)
                    sb.AppendLine("RimMind.Core.Prompt.HealthIssues".Translate(string.Join(", ", notable.Take(8))));
            }

            if (ctx.IncludeCapacities && data.Capacities.Count > 0)
            {
                var low = data.Capacities.Select(c => $"{c.Label}{c.Level * 100f:F0}%");
                sb.AppendLine("RimMind.Core.Prompt.Capacities".Translate(string.Join(", ", low)));
            }

            if (ctx.IncludeSkills && data.Skills.Count > 0)
            {
                var skills = data.Skills
                    .Where(s => s.Level >= ctx.MinSkillLevel)
                    .Select(s => $"{s.Label}({s.Level})")
                    .ToList();
                if (skills.Count > 0)
                    sb.AppendLine("RimMind.Core.Prompt.Skills".Translate(string.Join("  ", skills)));
            }

            if (ctx.IncludeCurrentJob)
            {
                string jobLabel = data.CurrentJobReport
                    ?? data.CurrentJobDefLabel
                    ?? "RimMind.Core.Prompt.None".Translate();
                sb.AppendLine("RimMind.Core.Prompt.CurrentJob".Translate(jobLabel));
            }

            if (ctx.IncludeWorkPriorities && data.WorkPriorities.Count > 0)
            {
                sb.AppendLine("RimMind.Core.Prompt.WorkPriorities".Translate(
                    string.Join("  ", data.WorkPriorities.Select(e => $"{e.Label}({e.Priority})"))));
            }

            if (ctx.IncludeTraits && data.TraitLabels.Count > 0)
                sb.AppendLine("RimMind.Core.Prompt.Traits".Translate(string.Join(", ", data.TraitLabels)));

            if (ctx.IncludeEquipment)
            {
                var parts = new List<string>();
                if (data.WeaponLabel != null)
                    parts.Add("RimMind.Core.Prompt.Weapon".Translate(data.WeaponLabel));
                if (data.ApparelLabels.Count > 0)
                    parts.Add("RimMind.Core.Prompt.Apparel".Translate(string.Join(", ", data.ApparelLabels)));
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
                sb.AppendLine("RimMind.Core.Prompt.Inventory".Translate(string.Join(", ", itemStrs)));
            }

            if (ctx.IncludeLocation && data.HasMap)
            {
                int temp = Mathf.RoundToInt(data.Temperature);
                sb.AppendLine("RimMind.Core.Prompt.Location".Translate(data.RoomLabel, $"{temp}"));
            }

            if (ctx.IncludeRelations && data.Relations.Count > 0)
            {
                var relParts = data.Relations.Select(r => $"{r.RelationLabel}({r.OtherName})");
                sb.AppendLine("RimMind.Core.Prompt.Relations".Translate(string.Join(", ", relParts)));
            }

            if (ctx.IncludeCombatStatus)
            {
                if (data.InCombat)
                {
                    string targetLabel = data.EnemyTargetLabel ?? "RimMind.Core.Prompt.Unknown".Translate();
                    sb.AppendLine("RimMind.Core.Prompt.InCombat".Translate(targetLabel));
                }
                if (data.Drafted)
                    sb.AppendLine("RimMind.Core.Prompt.Drafted".Translate());
            }

            if (ctx.IncludeSurroundings && pawn.Map != null)
            {
                string surroundings = BuildSurroundings(pawn);
                if (!string.IsNullOrEmpty(surroundings))
                    sb.AppendLine("RimMind.Core.Prompt.Surroundings".Translate(surroundings));
            }

            return sb.ToString().TrimEnd();
        }

        // ── 历史事件上下文 ────────────────────────────────────────────────────

        /// <summary>
        /// 从 WorldComponent 获取最近 N 条 AIStoryteller 历史记录。
        /// AIStoryteller 未安装时返回空字符串。
        /// </summary>
        private static string BuildSurroundings(Pawn pawn, int? radius = null, int? maxItems = null)
        {
            int r = radius ?? (RimMindCoreMod.Settings?.Context?.environmentScanRadius ?? 5);
            int m = maxItems ?? (RimMindCoreMod.Settings?.Context?.environmentMaxItems ?? 8);
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
                parts.Add("RimMind.Core.Prompt.SurroundingsBuildings".Translate(string.Join(", ", buildings.Distinct().Take(5))));
            if (items.Count > 0)
            {
                var itemStrs = items.OrderByDescending(kv => kv.Value)
                    .Take(m)
                    .Select(kv => $"{DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key)?.LabelCap ?? kv.Key}×{kv.Value}");
                parts.Add("RimMind.Core.Prompt.SurroundingsItems".Translate(string.Join(", ", itemStrs)));
            }
            if (animals.Count > 0)
                parts.Add("RimMind.Core.Prompt.SurroundingsAnimals".Translate(string.Join(", ", animals.Distinct().Take(4))));

            return parts.Count > 0 ? string.Join("  ", parts) : string.Empty;
        }

        private static string ThreatLabel(float wealth)
        {
            float high = RimMindCoreMod.Settings?.Context?.threatThresholdHigh ?? 200000f;
            float medium = RimMindCoreMod.Settings?.Context?.threatThresholdMedium ?? 100000f;
            float low = RimMindCoreMod.Settings?.Context?.threatThresholdLow ?? 50000f;

            float threatScale = 1f;
            try { threatScale = Find.Storyteller?.difficulty?.threatScale ?? 1f; } catch { }
            if (threatScale <= 0f) threatScale = 1f;

            string tier = ThreatClassifier.ClassifyThreatTier(wealth, high, medium, low, threatScale);
            return tier switch
            {
                "Extreme" => "RimMind.Core.Prompt.Threat.Extreme".Translate(),
                "High"    => "RimMind.Core.Prompt.Threat.High".Translate(),
                "Medium"  => "RimMind.Core.Prompt.Threat.Medium".Translate(),
                _         => "RimMind.Core.Prompt.Threat.Low".Translate()
            };
        }

        // ── 基础游戏状态收集（回退路径） ────────────────────────────────────

        /// <summary>
        /// 收集基础游戏状态文本，用于 ContextEngine 不可用时回退填充 game_state_info。
        /// 按 npcId 查找对应 Pawn，拼接地图上下文 + Pawn 上下文。
        /// </summary>
        public static string CollectBasicGameState(string npcId)
        {
            var sb = new StringBuilder();
            var pawn = NpcManager.FindPawnByNpcId(npcId);

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

        // ── PromptSection 版本 ──────────────────────────────────────────────

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

        // ── 精简 Pawn 上下文 ──────────────────────────────────────────────

        public static string BuildCompactPawnContext(Pawn pawn)
        {
            if (pawn == null) return string.Empty;

            var data = PawnDataExtractor.Extract(pawn);
            var sb = new StringBuilder();
            sb.Append(data.Name + "  ");

            var basics = new List<string>();
            basics.Add("RimMind.Core.Prompt.AgeFormat".Translate(data.Age));
            basics.Add(data.GenderLabel);
            basics.Add(data.RaceLabel);
            sb.AppendLine(string.Join("  ", basics));

            if (data.MoodString != null)
            {
                string moodLabel = data.InMentalState ? "RimMind.Core.Prompt.CompactMentalBreak".Translate()
                    : data.Downed ? "RimMind.Core.Prompt.CompactDowned".Translate()
                    : $"{data.MoodPercent:F0}%";
                sb.AppendLine("RimMind.Core.Prompt.CompactMood".Translate(moodLabel));
            }

            if (data.Hediffs.Count > 0)
            {
                var notable = new List<string>();
                foreach (var h in data.Hediffs)
                {
                    if (!h.IsBad || h.Severity < 0.05f || !h.Visible) continue;
                    string partLabel = h.PartLabel ?? "RimMind.Core.Prompt.FullBody".Translate();
                    notable.Add($"{partLabel}:{h.HediffLabel}");
                    if (notable.Count >= 3) break;
                }
                if (notable.Count > 0)
                    sb.AppendLine("RimMind.Core.Prompt.CompactHealth".Translate(string.Join(", ", notable)));
            }

            string jobLabel = data.CurrentJobReport
                ?? data.CurrentJobDefLabel
                ?? "RimMind.Core.Prompt.None".Translate();
            sb.AppendLine("RimMind.Core.Prompt.CompactJob".Translate(jobLabel));

            if (data.HasMap)
            {
                int temp = Mathf.RoundToInt(data.Temperature);
                sb.AppendLine("RimMind.Core.Prompt.CompactLocation".Translate(data.RoomLabel, $"{temp}"));
            }

            if (data.WeaponLabel != null)
                sb.AppendLine("RimMind.Core.Prompt.CompactWeapon".Translate(data.WeaponLabel));

            if (data.Drafted)
                sb.AppendLine("RimMind.Core.Prompt.Drafted".Translate());
            if (data.EnemyTargetLabel != null)
                sb.AppendLine("RimMind.Core.Prompt.InCombat".Translate(data.EnemyTargetLabel));

            return sb.ToString().TrimEnd();
        }

        // ── Per-Key 提取方法（供 ContextKeyRegistry 使用） ──────────────────

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
            if (data.TraitLabels.Count > 0)
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
                .OrderByDescending(s => s.Level)
                .Take(5)
                .Select(s => $"{s.Label}({s.Level})");
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
            return data.NearbyPawnNames.Count > 0 ? string.Join(", ", data.NearbyPawnNames) : "";
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
            return "RimMind.Core.Prompt.Colony.Status".Translate(data.ColonistCount, $"{data.ColonyWealth:F0}", data.ThreatCount);
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
            return notable.Count > 0 ? string.Join(", ", notable) : "RimMind.Core.Prompt.Health.Healthy".Translate();
        }

        public static string ExtractMood(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (data.MoodString == null) return "";
            if (data.InMentalState)
                return "RimMind.Core.Prompt.Mood.MentalBreak".Translate(data.MentalStateInspectLine ?? "");
            return $"{data.MoodPercent:F0}%";
        }

        public static string ExtractCurrentJob(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            return data.CurrentJobReport ?? data.CurrentJobDefLabel ?? "RimMind.Core.Prompt.Job.Idle".Translate();
        }

        public static string ExtractCombatStatus(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            var parts = new List<string>();
            if (data.Drafted) parts.Add("RimMind.Core.Prompt.Combat.Drafted".Translate());
            if (data.EnemyTargetLabel != null)
                parts.Add("RimMind.Core.Prompt.Combat.Fighting".Translate(data.EnemyTargetLabel));
            return parts.Count > 0 ? string.Join(" | ", parts) : "RimMind.Core.Prompt.Combat.NotInCombat".Translate();
        }

        public static string ExtractTargetInfo(Pawn pawn)
        {
            if (pawn == null) return "";
            var data = PawnDataExtractor.Extract(pawn);
            if (data.EnemyTargetLabel == null) return "";
            string label = data.EnemyTargetHpPercent.HasValue
                ? $"{data.EnemyTargetLabel} (HP:{data.EnemyTargetHpPercent.Value:F0}%)"
                : data.EnemyTargetLabel;
            return "RimMind.Core.Prompt.Target.Info".Translate(label);
        }
    }
}
