using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Runtime;

namespace RimMind.Application.Api
{
    public static partial class RimMindAPI
    {
        /// <summary>
        /// Facade for context-related operations: ScenarioRegistry, ContextKeyRegistry, SchemaRegistry.
        /// Sub-mods should use this instead of directly referencing
        /// RimMind.Application.Features.Context or RimMind.Presentation.Context.
        /// </summary>
        public static class Context
        {
            // ── ScenarioIds ──

            public static string ScenarioDialogue => ScenarioIds.Dialogue;
            public static string ScenarioDecision => ScenarioIds.Decision;
            public static string ScenarioPersonality => ScenarioIds.Personality;
            public static string ScenarioStoryteller => ScenarioIds.Storyteller;
            public static string ScenarioMemory => ScenarioIds.Memory;

            // ── ScenarioRegistry ──

            public static void RegisterScenario(string scenarioId, int defaultBaseRounds, string description,
                float[]? defaultEmbedding = null, float defaultBudget = 0.6f,
                L4Mode l4Mode = L4Mode.BudgetControlled, string[]? defaultExcludeKeys = null)
                => ScenarioRegistry.Register(scenarioId, defaultBaseRounds, description,
                    defaultEmbedding, defaultBudget, l4Mode, defaultExcludeKeys);

            public static bool UnregisterScenario(string scenarioId)
                => ScenarioRegistry.Unregister(scenarioId);

            public static int GetScenarioBaseRounds(string scenarioId)
                => ScenarioRegistry.GetBaseRounds(scenarioId);

            // ── ContextKeyRegistry (instance-based via RimMindRuntime) ──

            public static IContextKeyRegistry ContextKeys => RimMindRuntime.Instance.ContextKeys;
            public static IRelevanceTable RelevanceTable => RimMindRuntime.Instance.RelevanceTable;
            public static IRelevanceLearner ContextLearner => RimMindRuntime.Instance.ContextLearner;

            // ── SchemaRegistry ──

            public static string SchemaPersonalityOutput
                => SchemaRegistry.PersonalityOutput;

            public static string SchemaIncidentOutput
                => SchemaRegistry.IncidentOutput;

            public static string SchemaDarkMemoryOutput
                => SchemaRegistry.DarkMemoryOutput;

            public static void RegisterSchema(string key, string schema)
                => SchemaRegistry.Instance.Register(key, schema);

            public static string? FindSchema(string key)
                => SchemaRegistry.Instance.Find(key);
        }
    }
}
