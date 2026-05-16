using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal sealed class BudgetScheduler : IBudgetScheduler
    {
        private BudgetSchedulerConfig _config = new BudgetSchedulerConfig();

        public BudgetAllocation Schedule(List<KeyMeta> keys, string scenarioId, float budget, string? currentQuery)
        {
            var result = new BudgetAllocation();
            if (keys == null || keys.Count == 0) return result;

            float totalWeight = 0;
            var scored = new List<(KeyMeta key, float score)>();
            foreach (var key in keys)
            {
                float score = ComputeScore(key, scenarioId, currentQuery);
                scored.Add((key, score));
                totalWeight += score;
            }

            if (totalWeight <= 0) return result;

            scored.Sort((a, b) => b.score.CompareTo(a.score));

            float cumulative = 0;
            foreach (var (key, score) in scored)
            {
                float fraction = score / totalWeight;
                cumulative += fraction;

                var target = ChooseLayer(key.Layer, cumulative);
                AddToAllocation(result, target, key);
            }

            return result;
        }

        public void OnKeyUpdated(KeyMeta key) { }
        public void Calibrate(List<KeyMeta> keys) { }
        public void SetConfig(BudgetSchedulerConfig? config) { if (config != null) _config = config; }
        public BudgetSchedulerConfig GetConfig() => _config;

        private float ComputeScore(KeyMeta key, string scenarioId, string? query)
        {
            float baseScore = key.Priority * _config.W1 + key.AdaptivePriority * _config.W2;
            return Math.Max(0, baseScore);
        }

        private static ContextLayer ChooseLayer(ContextLayer declared, float cumulative)
        {
            if (declared != ContextLayer.L2_Environment) return declared;
            if (cumulative > 0.8f) return ContextLayer.L0_Static;
            if (cumulative > 0.5f) return ContextLayer.L1_Baseline;
            return ContextLayer.L2_Environment;
        }

        private static void AddToAllocation(BudgetAllocation alloc, ContextLayer layer, KeyMeta key)
        {
            switch (layer)
            {
                case ContextLayer.L0_Static: alloc.L0Keys.Add(key); break;
                case ContextLayer.L1_Baseline: alloc.L1Keys.Add(key); break;
                case ContextLayer.L2_Environment: alloc.L2Keys.Add(key); break;
                case ContextLayer.L3_State: alloc.L3Keys.Add(key); break;
                case ContextLayer.L5_Sensor: alloc.L5Keys.Add(key); break;
            }
        }
    }
}
