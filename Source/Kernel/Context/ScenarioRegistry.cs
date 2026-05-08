using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Abstractions;
using RimMind.Kernel.Logging;

namespace RimMind.Kernel.Context
{
    public enum L4Mode
    {
        BudgetControlled,
        MemoryOnly,
        NarrativeMemory,
        None
    }

    public class ScenarioMeta
    {
        public string Id = null!;
        public int DefaultBaseRounds;
        public string Description = null!;
        public float[]? DefaultEmbedding;
        public float DefaultBudget = 0.6f;
        public L4Mode L4Mode = L4Mode.BudgetControlled;
        public string[] DefaultExcludeKeys = new string[0];
    }

    public static class ScenarioRegistry
    {
        private static readonly ConcurrentDictionary<string, ScenarioMeta> _scenarios = new ConcurrentDictionary<string, ScenarioMeta>();
        private static bool _coreRegistered = false;

        private static ITranslationService? TranslationService =>
            RimMindServiceLocator.Get<ITranslationService>();

        private static string? T(string key, params object[] args)
        {
            return TranslationService?.Translate(key, args);
        }

        public static void Register(string scenarioId, int defaultBaseRounds, string description,
            float[]? defaultEmbedding = null, float defaultBudget = 0.6f,
            L4Mode l4Mode = L4Mode.BudgetControlled, string[]? defaultExcludeKeys = null)
        {
            if (_scenarios.ContainsKey(scenarioId))
            {
                RimMindLogger.Warning($"[RimMind-Core] Scenario '{scenarioId}' already registered, overwriting.");
            }
            _scenarios[scenarioId] = new ScenarioMeta
            {
                Id = scenarioId,
                DefaultBaseRounds = defaultBaseRounds,
                Description = description,
                DefaultEmbedding = defaultEmbedding,
                DefaultBudget = defaultBudget,
                L4Mode = l4Mode,
                DefaultExcludeKeys = defaultExcludeKeys ?? new string[0]
            };
        }

        public static bool Unregister(string scenarioId)
        {
            return _scenarios.TryRemove(scenarioId, out _);
        }

        public static ScenarioMeta? Get(string scenarioId)
        {
            return _scenarios.TryGetValue(scenarioId, out var meta) ? meta : null;
        }

        public static List<ScenarioMeta> GetAll()
        {
            return new List<ScenarioMeta>(_scenarios.Values);
        }

        public static void RegisterCoreScenarios()
        {
            if (_coreRegistered) return;
            _coreRegistered = true;
            Register(ScenarioIds.Dialogue, 10, T("RimMind.Core.Scenario.Dialogue") ?? "RimMind.Core.Scenario.Dialogue",
                defaultBudget: 0.6f, l4Mode: L4Mode.BudgetControlled,
                defaultExcludeKeys: new[] { "combat_status", "task_progress" });
            Register(ScenarioIds.Decision, 0, T("RimMind.Core.Scenario.Decision") ?? "RimMind.Core.Scenario.Decision",
                defaultBudget: 0.5f, l4Mode: L4Mode.None,
                defaultExcludeKeys: new string[0]);
            Register(ScenarioIds.Personality, 3, T("RimMind.Core.Scenario.Personality") ?? "RimMind.Core.Scenario.Personality",
                defaultBudget: 0.4f, l4Mode: L4Mode.MemoryOnly,
                defaultExcludeKeys: new[] { "combat_status" });
            Register(ScenarioIds.Storyteller, 8, T("RimMind.Core.Scenario.Storyteller") ?? "RimMind.Core.Scenario.Storyteller",
                defaultBudget: 0.7f, l4Mode: L4Mode.NarrativeMemory,
                defaultExcludeKeys: new[] { "npc_identity", "pawn_base_info", "fixed_relations",
                    "ideology", "skills_summary", "health", "mood", "current_job",
                    "combat_status", "target_info", "task_progress", "npc_commands" });
            Register(ScenarioIds.Memory, 0, T("RimMind.Core.Scenario.Memory") ?? "RimMind.Core.Scenario.Memory",
                defaultBudget: 0.4f, l4Mode: L4Mode.None,
                defaultExcludeKeys: new[] { "combat_status", "current_job", "mood",
                    "task_progress", "npc_commands", "target_info" });
        }

        public static int GetBaseRounds(string scenarioId)
        {
            var meta = Get(scenarioId);
            return meta?.DefaultBaseRounds ?? 6;
        }

        public static void Clear()
        {
            _scenarios.Clear();
            _coreRegistered = false;
        }
    }

    public static class ScenarioIds
    {
        public const string Dialogue = "Dialogue";
        public const string Decision = "Decision";
        public const string Personality = "Personality";
        public const string Storyteller = "Storyteller";
        public const string Memory = "Memory";
    }
}
