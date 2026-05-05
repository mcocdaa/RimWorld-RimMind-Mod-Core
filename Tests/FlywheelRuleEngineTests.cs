using System.Collections.Generic;
using Xunit;

namespace RimMind.Core.Tests
{
    public class FlywheelRuleEngineTests
    {
        private static TelemetryRecord MakeRecord(
            int totalTokens = 1000,
            float budgetValue = 1.0f,
            bool parseSuccess = true,
            int keysIncluded = 5,
            int keysTrimmed = 1,
            Dictionary<string, float>? cacheHitRate = null)
        {
            return new TelemetryRecord
            {
                NpcId = "test",
                Scenario = "Decision",
                TotalTokens = totalTokens,
                BudgetValue = budgetValue,
                ResponseParseSuccess = parseSuccess,
                KeysIncluded = new string[keysIncluded],
                KeysTrimmed = new string[keysTrimmed],
                CacheHitRate = cacheHitRate,
            };
        }

        [Fact]
        public void ComputeAvgBudgetUtilization_NormalCase()
        {
            float budgetLimit = 1.0f * 4000f;
            float expected = 1000f / budgetLimit;
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgBudgetUtilization(
                new List<TelemetryRecord>
                {
                    MakeRecord(totalTokens: 1000, budgetValue: 1.0f),
                });
            Assert.Equal(expected, actual, 4);
        }

        [Fact]
        public void ComputeAvgBudgetUtilization_ZeroBudget_Returns0()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgBudgetUtilization(
                new List<TelemetryRecord>
                {
                    MakeRecord(totalTokens: 1000, budgetValue: 0f),
                });
            Assert.Equal(0f, actual, 4);
        }

        [Fact]
        public void ComputeAvgBudgetUtilization_EmptyList_Returns0()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgBudgetUtilization(
                new List<TelemetryRecord>());
            Assert.Equal(0f, actual, 4);
        }

        [Fact]
        public void ComputeAvgCacheHitRate_MultipleLayers()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgCacheHitRate(
                new List<TelemetryRecord>
                {
                    MakeRecord(cacheHitRate: new Dictionary<string, float>
                    {
                        { "L0_identity", 0.8f },
                        { "L1_recent", 0.6f },
                    }),
                    MakeRecord(cacheHitRate: new Dictionary<string, float>
                    {
                        { "L0_identity", 1.0f },
                    }),
                });
            Assert.Equal((0.8f + 0.6f + 1.0f) / 3f, actual, 4);
        }

        [Fact]
        public void ComputeAvgCacheHitRate_NoCacheData_Returns0()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgCacheHitRate(
                new List<TelemetryRecord>
                {
                    MakeRecord(),
                });
            Assert.Equal(0f, actual, 4);
        }

        [Fact]
        public void ComputeAvgParseSuccessRate_AllSuccess()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgParseSuccessRate(
                new List<TelemetryRecord>
                {
                    MakeRecord(parseSuccess: true),
                    MakeRecord(parseSuccess: true),
                    MakeRecord(parseSuccess: true),
                });
            Assert.Equal(1.0f, actual, 4);
        }

        [Fact]
        public void ComputeAvgParseSuccessRate_Mixed()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgParseSuccessRate(
                new List<TelemetryRecord>
                {
                    MakeRecord(parseSuccess: true),
                    MakeRecord(parseSuccess: false),
                    MakeRecord(parseSuccess: true),
                });
            Assert.Equal(2f / 3f, actual, 4);
        }

        [Fact]
        public void ComputeAvgParseSuccessRate_Empty_Returns1()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgParseSuccessRate(
                new List<TelemetryRecord>());
            Assert.Equal(1f, actual, 4);
        }

        [Fact]
        public void ComputeAvgTrimRatio_NormalCase()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgTrimRatio(
                new List<TelemetryRecord>
                {
                    MakeRecord(keysIncluded: 8, keysTrimmed: 2),
                });
            Assert.Equal(0.2f, actual, 4);
        }

        [Fact]
        public void ComputeAvgTrimRatio_NoKeys_Returns0()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgTrimRatio(
                new List<TelemetryRecord>
                {
                    MakeRecord(keysIncluded: 0, keysTrimmed: 0),
                });
            Assert.Equal(0f, actual, 4);
        }

        [Fact]
        public void ComputeAvgTrimRatio_AllTrimmed()
        {
            float actual = FlywheelRuleEngineTests_Helper.ComputeAvgTrimRatio(
                new List<TelemetryRecord>
                {
                    MakeRecord(keysIncluded: 0, keysTrimmed: 5),
                });
            Assert.Equal(1.0f, actual, 4);
        }
    }

    public static class FlywheelRuleEngineTests_Helper
    {
        public static float ComputeAvgBudgetUtilization(List<TelemetryRecord> records)
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

        public static float ComputeAvgCacheHitRate(List<TelemetryRecord> records)
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

        public static float ComputeAvgParseSuccessRate(List<TelemetryRecord> records)
        {
            int success = 0;
            int total = 0;
            foreach (var r in records)
            {
                total++;
                if (r.ResponseParseSuccess) success++;
            }
            return total > 0 ? (float)success / total : 1f;
        }

        public static float ComputeAvgTrimRatio(List<TelemetryRecord> records)
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
