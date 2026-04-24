using System;
using System.Collections.Generic;
using Verse;

namespace RimMind.Core.Flywheel
{
    public class ParameterRecommendation
    {
        public string Target;
        public float CurrentValue;
        public float RecommendedValue;
        public float Confidence;
        public string Reason;
    }

    public static class FlywheelRuleEngine
    {
        public static List<ParameterRecommendation> Analyze(List<TelemetryRecord> records)
        {
            var recommendations = new List<ParameterRecommendation>();
            if (records == null || records.Count == 0) return recommendations;

            var store = FlywheelParameterStore.Instance;

            float avgBudgetUtil = ComputeAvgBudgetUtilization(records);
            float avgCacheHitRate = ComputeAvgCacheHitRate(records);
            float avgParseSuccessRate = ComputeAvgParseSuccessRate(records);
            float avgTrimRatio = ComputeAvgTrimRatio(records);

            if (avgBudgetUtil < 0.5f && store != null)
            {
                float currentBudget = store.Get("TotalBudget");
                recommendations.Add(new ParameterRecommendation
                {
                    Target = "TotalBudget",
                    CurrentValue = currentBudget,
                    RecommendedValue = currentBudget * 0.8f,
                    Confidence = 0.7f,
                    Reason = $"Budget utilization only {avgBudgetUtil:P0}, consider reducing TotalBudget"
                });
            }

            if (avgBudgetUtil > 0.95f && store != null)
            {
                float currentBudget = store.Get("TotalBudget");
                recommendations.Add(new ParameterRecommendation
                {
                    Target = "TotalBudget",
                    CurrentValue = currentBudget,
                    RecommendedValue = currentBudget * 1.2f,
                    Confidence = 0.8f,
                    Reason = $"Budget utilization {avgBudgetUtil:P0}, risk of over-trimming"
                });
            }

            if (avgCacheHitRate < 0.3f && store != null)
            {
                float currentAlpha = store.Get("Alpha");
                recommendations.Add(new ParameterRecommendation
                {
                    Target = "Alpha",
                    CurrentValue = currentAlpha,
                    RecommendedValue = currentAlpha * 0.5f,
                    Confidence = 0.6f,
                    Reason = $"Cache hit rate only {avgCacheHitRate:P0}, reduce Alpha to slow priority decay"
                });
            }

            if (avgTrimRatio > 0.4f && store != null)
            {
                float currentW1 = store.Get("w1");
                recommendations.Add(new ParameterRecommendation
                {
                    Target = "w1",
                    CurrentValue = currentW1,
                    RecommendedValue = Math.Min(0.7f, currentW1 + 0.1f),
                    Confidence = 0.65f,
                    Reason = $"Trim ratio {avgTrimRatio:P0} too high, increase w1 to prioritize stability"
                });
            }

            if (avgParseSuccessRate < 0.8f && store != null)
            {
                float currentReserve = store.Get("ReserveForOutput");
                recommendations.Add(new ParameterRecommendation
                {
                    Target = "ReserveForOutput",
                    CurrentValue = currentReserve,
                    RecommendedValue = currentReserve * 1.3f,
                    Confidence = 0.7f,
                    Reason = $"Parse success rate {avgParseSuccessRate:P0}, increase output reserve"
                });
            }

            return recommendations;
        }

        private static float ComputeAvgBudgetUtilization(List<TelemetryRecord> records)
        {
            float sum = 0;
            int count = 0;
            foreach (var r in records)
            {
                if (r.BudgetValue > 0 && r.TotalTokens > 0)
                {
                    float budgetLimit = r.BudgetValue * 4000f;
                    if (budgetLimit > 0)
                    {
                        sum += r.TotalTokens / budgetLimit;
                        count++;
                    }
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        private static float ComputeAvgCacheHitRate(List<TelemetryRecord> records)
        {
            float sum = 0;
            int count = 0;
            foreach (var r in records)
            {
                if (r.CacheHitRate != null && r.CacheHitRate.Count > 0)
                {
                    foreach (var kvp in r.CacheHitRate)
                    {
                        sum += kvp.Value;
                        count++;
                    }
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        private static float ComputeAvgParseSuccessRate(List<TelemetryRecord> records)
        {
            int success = 0;
            int total = 0;
            foreach (var r in records)
            {
                total++;
                if (r.ResponseParseSuccess)
                    success++;
            }
            return total > 0 ? (float)success / total : 1f;
        }

        private static float ComputeAvgTrimRatio(List<TelemetryRecord> records)
        {
            float sum = 0;
            int count = 0;
            foreach (var r in records)
            {
                int included = r.KeysIncluded?.Length ?? 0;
                int trimmed = r.KeysTrimmed?.Length ?? 0;
                int total = included + trimmed;
                if (total > 0)
                {
                    sum += (float)trimmed / total;
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }
    }
}
