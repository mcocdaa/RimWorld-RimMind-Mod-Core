using System.Collections.Generic;
using RimMind.Kernel.Flywheel;
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
                LayerTokenBreakdown = new Dictionary<string, int>(),
            };
        }

        [Fact]
        public void Analyze_EmptyList_ReturnsEmptyRecommendations()
        {
            var result = FlywheelRuleEngine.Analyze(new List<TelemetryRecord>());
            Assert.Empty(result);
        }

        [Fact]
        public void Analyze_NullList_ReturnsEmptyRecommendations()
        {
            var result = FlywheelRuleEngine.Analyze(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void Analyze_LowBudgetUtilization_ReturnsBudgetRecommendation()
        {
            RimMindCoreMod.Settings = new AICoreSettings();
            try
            {
                var store = FlywheelParameterStore.Instance;
                if (store == null)
                {
                    return;
                }
                var records = new List<TelemetryRecord>
                {
                    MakeRecord(totalTokens: 100, budgetValue: 1.0f),
                };
                var result = FlywheelRuleEngine.Analyze(records);
                Assert.NotEmpty(result);
                Assert.Contains(result, r => r.Target == "TotalBudget");
            }
            finally
            {
                RimMindCoreMod.Settings = null;
            }
        }

        [Fact]
        public void Analyze_HighTrimRatio_ReturnsW1Recommendation()
        {
            RimMindCoreMod.Settings = new AICoreSettings();
            try
            {
                if (FlywheelParameterStore.Instance == null) return;
                var records = new List<TelemetryRecord>
                {
                    MakeRecord(keysIncluded: 2, keysTrimmed: 8),
                };
                var result = FlywheelRuleEngine.Analyze(records);
                Assert.Contains(result, r => r.Target == "w1");
            }
            finally
            {
                RimMindCoreMod.Settings = null;
            }
        }

        [Fact]
        public void Analyze_LowCacheHitRate_ReturnsAlphaRecommendation()
        {
            RimMindCoreMod.Settings = new AICoreSettings();
            try
            {
                if (FlywheelParameterStore.Instance == null) return;
                var records = new List<TelemetryRecord>
                {
                    MakeRecord(cacheHitRate: new Dictionary<string, float>
                    {
                        { "L0_identity", 0.1f },
                    }),
                };
                var result = FlywheelRuleEngine.Analyze(records);
                Assert.Contains(result, r => r.Target == "Alpha");
            }
            finally
            {
                RimMindCoreMod.Settings = null;
            }
        }

        [Fact]
        public void Analyze_LowParseSuccessRate_ReturnsReserveRecommendation()
        {
            RimMindCoreMod.Settings = new AICoreSettings();
            try
            {
                var store = FlywheelParameterStore.Instance;
                if (store == null)
                {
                    return;
                }
                var records = new List<TelemetryRecord>
                {
                    MakeRecord(parseSuccess: false),
                    MakeRecord(parseSuccess: false),
                    MakeRecord(parseSuccess: false),
                };
                var result = FlywheelRuleEngine.Analyze(records);
                Assert.Contains(result, r => r.Target == "ReserveForOutput");
            }
            finally
            {
                RimMindCoreMod.Settings = null;
            }
        }

        [Fact]
        public void Analyze_HealthyMetrics_ReturnsFewerRecommendations()
        {
            RimMindCoreMod.Settings = new AICoreSettings();
            try
            {
                if (FlywheelParameterStore.Instance == null) return;
                var records = new List<TelemetryRecord>
                {
                    MakeRecord(
                        totalTokens: 3000,
                        budgetValue: 1.0f,
                        parseSuccess: true,
                        keysIncluded: 8,
                        keysTrimmed: 1,
                        cacheHitRate: new Dictionary<string, float> { { "L0", 0.8f } }),
                };
                var result = FlywheelRuleEngine.Analyze(records);
                Assert.True(result.Count <= 2);
            }
            finally
            {
                RimMindCoreMod.Settings = null;
            }
        }
    }
}
