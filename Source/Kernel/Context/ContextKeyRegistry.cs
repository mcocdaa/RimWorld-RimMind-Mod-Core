using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Contracts;
using RimMind.Contracts.Client;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Context;
using RimMind.Contracts.Abstractions;
using RimMind.Kernel.Logging;

namespace RimMind.Kernel.Context
{
    public static class ContextKeyRegistry
    {
        private static readonly ConcurrentDictionary<string, KeyMeta> _keys = new ConcurrentDictionary<string, KeyMeta>();
        private static bool _coreRegistered = false;
        private static string _currentScenario = string.Empty;
        public static string CurrentScenario { get => _currentScenario; set => _currentScenario = value; }

        private static string? _currentSpeakerName;
        public static string? CurrentSpeakerName { get => _currentSpeakerName; set => _currentSpeakerName = value; }

        private static bool _currentIsMonologue;
        public static bool CurrentIsMonologue { get => _currentIsMonologue; set => _currentIsMonologue = value; }

        private static ITranslationService? TranslationService =>
            RimMindServiceLocator.Get<ITranslationService>();

        private static string? T(string key, params object[] args)
        {
            return TranslationService?.Translate(key, args);
        }

        public static void Register(string key, ContextLayer layer, float priority,
            Func<object, List<ContextEntry>> provider, string ownerMod,
            bool isIndexable = false, float[]? keyEmbedding = null)
        {
            if (_keys.ContainsKey(key))
            {
                var old = _keys[key];
                RimMindLogger.Warning($"[RimMind-Core] ContextKey '{key}' registered by '{old.OwnerMod}' " +
                    $"overwritten by '{ownerMod}'.");
            }
            _keys[key] = new KeyMeta(key, layer, priority, provider, ownerMod,
                isIndexable, keyEmbedding);
        }

        public static bool Unregister(string key)
        {
            return _keys.TryRemove(key, out _);
        }

        public static List<KeyMeta> GetAll()
        {
            return new List<KeyMeta>(_keys.Values);
        }

        public static List<KeyMeta> GetByLayer(ContextLayer layer)
        {
            var result = new List<KeyMeta>();
            foreach (var kvp in _keys)
            {
                if (kvp.Value.Layer == layer)
                    result.Add(kvp.Value);
            }
            return result;
        }

        public static KeyMeta? GetKey(string key)
        {
            return _keys.TryGetValue(key, out var meta) ? meta : null;
        }

        private static List<ContextEntry> WrapEntry(string value)
        {
            return string.IsNullOrEmpty(value) ? new List<ContextEntry>() : new List<ContextEntry> { new ContextEntry(value) };
        }

        public static void RegisterCoreKeys()
        {
            if (_coreRegistered) return;
            _coreRegistered = true;

            var ctx = RimMindServiceLocator.Get<IContextKeyProvider>();

            Register("system_instruction", ContextLayer.L0_Static, 1.0f,
                pawnObj =>
                {
                    if (CurrentScenario == ScenarioIds.Storyteller)
                        return WrapEntry("You are the RimWorld storyteller AI. Based on the colony's current situation, select the most appropriate incident event. " +
                            "Consider colony wealth, threat level, food supply, colonist count, and recent events. " +
                            "Output must be valid JSON matching the IncidentOutput schema.");
                    if (CurrentScenario == ScenarioIds.Decision)
                        return WrapEntry("");
                    if (CurrentScenario == ScenarioIds.Dialogue)
                        return WrapEntry("");
                    var pawn = pawnObj as Verse.Pawn;
                    if (pawn == null) return WrapEntry("");
                    var profile = RimMindServiceLocator.Get<INpcManager>()?.GetNpc($"NPC-{pawn.thingIDNumber}");
                    return WrapEntry(profile?.SystemPrompt ?? "");
                }, "Core");
            Register("npc_identity", ContextLayer.L0_Static, 1.0f,
                pawnObj =>
                {
                    if (CurrentScenario == ScenarioIds.Decision)
                        return WrapEntry("");
                    if (CurrentScenario == ScenarioIds.Dialogue)
                        return WrapEntry("");
                    var pawn = pawnObj as Verse.Pawn;
                    if (pawn == null) return WrapEntry("");
                    var profile = RimMindServiceLocator.Get<INpcManager>()?.GetNpc($"NPC-{pawn.thingIDNumber}");
                    if (profile == null) return WrapEntry("");
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine(T("RimMind.Core.Prompt.Identity.Name", profile.Name) ?? "");
                    if (!string.IsNullOrEmpty(profile.ShortName))
                        sb.AppendLine(T("RimMind.Core.Prompt.Identity.ShortName", profile.ShortName) ?? "");
                    if (!string.IsNullOrEmpty(profile.CharacterDescription))
                        sb.AppendLine(T("RimMind.Core.Prompt.Identity.Description", profile.CharacterDescription) ?? "");
                    return WrapEntry(sb.ToString().TrimEnd());
                }, "Core");
            Register("npc_commands", ContextLayer.L0_Static, 1.0f,
                pawnObj =>
                {
                    if (CurrentScenario == ScenarioIds.Decision) return WrapEntry("");
                    if (CurrentScenario == ScenarioIds.Dialogue) return WrapEntry("");
                    var pawn = pawnObj as Verse.Pawn;
                    if (pawn == null) return WrapEntry("");
                    var profile = RimMindServiceLocator.Get<INpcManager>()?.GetNpc($"NPC-{pawn.thingIDNumber}");
                    if (profile == null || profile.Commands.Count == 0) return WrapEntry("");
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine(T("RimMind.Core.Prompt.Commands.Available") ?? "");
                    foreach (var cmd in profile.Commands)
                        sb.AppendLine(T("RimMind.Core.Prompt.Commands.Entry", cmd.Name, cmd.Description) ?? "");
                    return WrapEntry(sb.ToString().TrimEnd());
                }, "Core");
            Register("world_rules", ContextLayer.L0_Static, 1.0f,
                pawnObj =>
                {
                    if (CurrentScenario == ScenarioIds.Storyteller)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Storyteller rules:");
                        sb.AppendLine("- Only select from available RimWorld incident definitions");
                        sb.AppendLine("- Consider threat level relative to colony wealth");
                        sb.AppendLine("- Balance positive and negative events");
                        sb.AppendLine("- Food shortages should trigger related events");
                        sb.AppendLine("- Low mood colonists may need positive events");
                        return WrapEntry(sb.ToString().TrimEnd());
                    }
                    var sb2 = new System.Text.StringBuilder();
                    sb2.AppendLine(T("RimMind.Core.Prompt.WorldRules.Header") ?? "");
                    sb2.AppendLine(T("RimMind.Core.Prompt.WorldRules.Survival") ?? "");
                    sb2.AppendLine(T("RimMind.Core.Prompt.WorldRules.Combat") ?? "");
                    sb2.AppendLine(T("RimMind.Core.Prompt.WorldRules.Relationships") ?? "");
                    sb2.AppendLine(T("RimMind.Core.Prompt.WorldRules.Weather") ?? "");
                    sb2.AppendLine(T("RimMind.Core.Prompt.WorldRules.Medical") ?? "");
                    return WrapEntry(sb2.ToString().TrimEnd());
                }, "Core");
            Register("npc_task_instruction", ContextLayer.L0_Static, 1.0f,
                pawnObj =>
                {
                    if (CurrentScenario == ScenarioIds.Storyteller)
                        return WrapEntry("Select the most fitting incident for the colony's current state. Return structured JSON with defName, reason, and optional params.");
                    if (CurrentScenario == ScenarioIds.Decision)
                        return WrapEntry(T("RimMind.Core.Prompt.TaskInstruction.WorldOnly") ?? "");
                    if (CurrentScenario == ScenarioIds.Dialogue)
                        return WrapEntry(T("RimMind.Core.Prompt.TaskInstruction.WorldOnly") ?? "");
                    return WrapEntry(T("RimMind.Core.Prompt.TaskInstruction.Base") ?? "");
                }, "Core");

            if (ctx == null) return;

            Register("map_structure", ContextLayer.L1_Baseline, 0.95f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn?.Map != null ? ctx.BuildMapContextEntries(pawn.Map) : new List<ContextEntry>(); }, "Core");
            Register("pawn_base_info", ContextLayer.L1_Baseline, 0.95f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractPawnBaseInfo(pawn)) : WrapEntry(""); }, "Core");
            Register("fixed_relations", ContextLayer.L1_Baseline, 0.9f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractFixedRelations(pawn)) : WrapEntry(""); }, "Core");
            Register("ideology", ContextLayer.L1_Baseline, 0.9f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractIdeology(pawn)) : WrapEntry(""); }, "Core");
            Register("skills_summary", ContextLayer.L1_Baseline, 0.85f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractSkillsSummary(pawn)) : WrapEntry(""); }, "Core");

            Register("current_area", ContextLayer.L2_Environment, 0.7f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractCurrentArea(pawn)) : WrapEntry(""); }, "Core");
            Register("weather", ContextLayer.L2_Environment, 0.6f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractWeather(pawn)) : WrapEntry(""); }, "Core");
            Register("time_of_day", ContextLayer.L2_Environment, 0.65f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractTimeOfDay(pawn)) : WrapEntry(""); }, "Core");
            Register("nearby_pawns", ContextLayer.L2_Environment, 0.7f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractNearbyPawns(pawn)) : WrapEntry(""); }, "Core");
            Register("season", ContextLayer.L2_Environment, 0.5f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractSeason(pawn)) : WrapEntry(""); }, "Core");
            Register("colony_status", ContextLayer.L2_Environment, 0.6f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractColonyStatus(pawn)) : WrapEntry(""); }, "Core");

            Register("health", ContextLayer.L3_State, 0.3f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractHealth(pawn)) : WrapEntry(""); }, "Core");
            Register("mood", ContextLayer.L3_State, 0.3f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractMood(pawn)) : WrapEntry(""); }, "Core");
            Register("current_job", ContextLayer.L3_State, 0.25f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractCurrentJob(pawn)) : WrapEntry(""); }, "Core");
            Register("combat_status", ContextLayer.L3_State, 0.2f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractCombatStatus(pawn)) : WrapEntry(""); }, "Core");
            Register("target_info", ContextLayer.L3_State, 0.15f,
                pawnObj => { var pawn = pawnObj as Verse.Pawn; return pawn != null ? WrapEntry(ctx.ExtractTargetInfo(pawn)) : WrapEntry(""); }, "Core");
            Register("task_progress", ContextLayer.L3_State, 0.2f,
                pawnObj =>
                {
                    var pawn = pawnObj as Verse.Pawn;
                    if (pawn == null) return WrapEntry("");
                    return WrapEntry(ctx.ExtractTaskProgress(pawn));
                }, "Core");
        }

        public static void Clear()
        {
            _keys.Clear();
            _coreRegistered = false;
        }
    }
}
