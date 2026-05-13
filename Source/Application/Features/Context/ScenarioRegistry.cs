using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;

namespace RimMind.Application.Features.Context
{
    internal sealed class ScenarioRegistry
    {
        private readonly ConcurrentDictionary<string, ScenarioConfig> _scenarios
            = new ConcurrentDictionary<string, ScenarioConfig>();
        private readonly ILogSink? _log;

        public ScenarioRegistry(ILogSink? log = null) { _log = log; }

        public void Register(string id, ScenarioConfig config) => _scenarios[id] = config;
        public ScenarioConfig? Find(string id) => _scenarios.TryGetValue(id, out var c) ? c : null;
        public IReadOnlyDictionary<string, ScenarioConfig> All => _scenarios;

        public void Reset() => _scenarios.Clear();
    }

    public class ScenarioConfig
    {
        public string Id { get; set; } = "";
        public float DefaultBudget { get; set; } = 1.0f;
        public int MaxTokens { get; set; } = 800;
        public float Temperature { get; set; } = 0.7f;
        public string? SystemPromptOverride { get; set; }
        public string[]? RequiredKeys { get; set; }
    }
}
