using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal sealed class RelevanceTable : IRelevanceProvider
    {
        private readonly Dictionary<(string scenario, string key), float> _table
            = new Dictionary<(string, string), float>();
        private readonly ILogSink? _log;

        public RelevanceTable(ILogSink? log = null) { _log = log; }

        public float ComputeRelevance(string scenarioId, string npcId, KeyMeta key)
        {
            if (_table.TryGetValue((scenarioId, key.Key), out float score))
                return score;
            return key.Priority;
        }

        public void SetRelevance(string scenarioId, string key, float score)
        {
            _table[(scenarioId, key)] = Math.Max(0f, Math.Min(1f, score));
        }

        public void Clear() => _table.Clear();
    }
}
