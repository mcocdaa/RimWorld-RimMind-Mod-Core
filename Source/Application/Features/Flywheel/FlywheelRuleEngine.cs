using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Models.Context;

namespace RimMind.Application.Features.Flywheel
{
    internal sealed class FlywheelRuleEngine
    {
        private readonly IFlywheelParameterStore _store;
        private readonly ILogSink? _log;

        public FlywheelRuleEngine(IFlywheelParameterStore store, ILogSink? log = null)
        {
            _store = store;
            _log = log;
        }

        public List<ParameterRecommendation> Evaluate(Dictionary<string, float> metrics)
        {
            var recommendations = new List<ParameterRecommendation>();
            if (metrics == null) return recommendations;

            if (metrics.TryGetValue("avg_tokens_used", out var avgTokens) && avgTokens > 0)
            {
                float budget = _store.Get("ContextBudget");
                float ratio = avgTokens / (budget * 800);
                if (ratio > 0.9f)
                {
                    recommendations.Add(new ParameterRecommendation
                    {
                        Target = "ContextBudget",
                        CurrentValue = budget,
                        RecommendedValue = Math.Min(2.0f, budget * 1.1f),
                        Confidence = 0.8f,
                        Reason = "Token usage ratio > 90%, increasing budget"
                    });
                }
                else if (ratio < 0.3f)
                {
                    recommendations.Add(new ParameterRecommendation
                    {
                        Target = "ContextBudget",
                        CurrentValue = budget,
                        RecommendedValue = Math.Max(0.3f, budget * 0.9f),
                        Confidence = 0.6f,
                        Reason = "Token usage ratio < 30%, decreasing budget"
                    });
                }
            }

            return recommendations;
        }
    }
}
