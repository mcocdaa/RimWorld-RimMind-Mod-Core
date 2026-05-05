using System;
using System.Collections.Generic;
using RimMind.Core.Context;
using Xunit;

namespace RimMind.Core.Tests
{
    public class BudgetSchedulerTests
    {
        private static KeyMeta MakeKey(string key, ContextLayer layer, float priority)
        {
            return new KeyMeta(key, layer, priority, _ => new List<ContextEntry>(), "TestMod");
        }

        [Fact]
        public void Schedule_L0Keys_AlwaysIncluded()
        {
            var scheduler = new BudgetScheduler();
            var keys = new List<KeyMeta>
            {
                MakeKey("static1", ContextLayer.L0_Static, 0.9f),
                MakeKey("static2", ContextLayer.L0_Static, 0.5f),
            };

            var result = scheduler.Schedule(keys, ScenarioIds.Decision, 0.05f, null);
            Assert.Equal(2, result.L0Keys.Count);
        }

        [Fact]
        public void Schedule_L1Keys_Included_WhenBudgetAbove02()
        {
            var scheduler = new BudgetScheduler();
            var keys = new List<KeyMeta>
            {
                MakeKey("base1", ContextLayer.L1_Baseline, 0.8f),
            };

            var result = scheduler.Schedule(keys, ScenarioIds.Decision, 0.3f, null);
            Assert.Single(result.L1Keys);

            var resultLow = scheduler.Schedule(keys, ScenarioIds.Decision, 0.1f, null);
            Assert.Empty(resultLow.L1Keys);
        }

        [Fact]
        public void Schedule_L5Keys_Included_WhenBudgetAbove03()
        {
            var scheduler = new BudgetScheduler();
            var keys = new List<KeyMeta>
            {
                MakeKey("sensor1", ContextLayer.L5_Sensor, 0.9f),
            };

            var result = scheduler.Schedule(keys, ScenarioIds.Decision, 0.5f, null);
            Assert.Single(result.L5Keys);

            var resultLow = scheduler.Schedule(keys, ScenarioIds.Decision, 0.2f, null);
            Assert.Empty(resultLow.L5Keys);
        }

        [Fact]
        public void Schedule_L3Keys_Truncated_WhenBudgetBelow01()
        {
            RelevanceTable.Clear();
            RelevanceTable.Register(ScenarioIds.Decision, "health", 1.0f);
            RelevanceTable.Register(ScenarioIds.Decision, "mood", 1.0f);
            RelevanceTable.Register(ScenarioIds.Decision, "combat_status", 0.5f);

            var scheduler = new BudgetScheduler();
            var keys = new List<KeyMeta>
            {
                MakeKey("health", ContextLayer.L3_State, 1.0f),
                MakeKey("mood", ContextLayer.L3_State, 1.0f),
                MakeKey("combat_status", ContextLayer.L3_State, 0.5f),
            };

            var result = scheduler.Schedule(keys, ScenarioIds.Decision, 0.05f, null);
            Assert.Single(result.L3Keys);
            Assert.Equal("health", result.L3Keys[0].Key);
        }

        [Fact]
        public void Schedule_UseFullValue_WhenBudgetAbove07()
        {
            var scheduler = new BudgetScheduler();
            var result = scheduler.Schedule(new List<KeyMeta>(), ScenarioIds.Decision, 0.8f, null);
            Assert.True(result.UseFullValue);
            Assert.False(result.UseDiff);
        }

        [Fact]
        public void Schedule_UseDiff_WhenBudgetBetween02And07()
        {
            var scheduler = new BudgetScheduler();
            var result = scheduler.Schedule(new List<KeyMeta>(), ScenarioIds.Decision, 0.5f, null);
            Assert.False(result.UseFullValue);
            Assert.True(result.UseDiff);
        }

        [Fact]
        public void Schedule_Neither_WhenBudgetBelow02()
        {
            var scheduler = new BudgetScheduler();
            var result = scheduler.Schedule(new List<KeyMeta>(), ScenarioIds.Decision, 0.1f, null);
            Assert.False(result.UseFullValue);
            Assert.False(result.UseDiff);
        }

        [Fact]
        public void OnKeyUpdated_IncreasesAdaptivePriority_WhenAlphaSmall()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("test", ContextLayer.L2_Environment, 0.5f);

            scheduler.OnKeyUpdated(key);
            Assert.Equal(1, key.UpdateCount);
            Assert.True(key.AdaptivePriority > 0.5f);
        }

        [Fact]
        public void OnKeyUpdated_MultipleUpdates_ConvergesToLowerPriority()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("test", ContextLayer.L2_Environment, 0.9f);

            for (int i = 0; i < 100; i++)
                scheduler.OnKeyUpdated(key);

            Assert.True(key.AdaptivePriority < 0.9f);
            Assert.True(key.AdaptivePriority > 0f);
        }

        [Fact]
        public void OnKeyUpdated_LargeAlpha_DecreasesAdaptivePriority()
        {
            var scheduler = new BudgetScheduler(new BudgetSchedulerConfig { Alpha = 10f, AlphaSmooth = 0.3f });
            var key = MakeKey("test", ContextLayer.L2_Environment, 0.9f);

            scheduler.OnKeyUpdated(key);
            Assert.True(key.AdaptivePriority < 0.9f);
        }

        [Fact]
        public void Calibrate_HalvesUpdateCount()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("test", ContextLayer.L2_Environment, 0.9f);
            for (int i = 0; i < 10; i++) scheduler.OnKeyUpdated(key);
            Assert.Equal(10, key.UpdateCount);

            scheduler.Calibrate(new List<KeyMeta> { key });
            Assert.Equal(5, key.UpdateCount);
        }

        [Fact]
        public void SetConfig_UpdatesConfig()
        {
            var scheduler = new BudgetScheduler();
            var newConfig = new BudgetSchedulerConfig { W1 = 0.8f, W2 = 0.2f };
            scheduler.SetConfig(newConfig);

            var config = scheduler.GetConfig();
            Assert.Equal(0.8f, config.W1);
            Assert.Equal(0.2f, config.W2);
        }

        [Fact]
        public void SetConfig_Null_ResetsToDefault()
        {
            var scheduler = new BudgetScheduler();
            scheduler.SetConfig(null);

            var config = scheduler.GetConfig();
            Assert.Equal(0.4f, config.W1);
        }

        [Fact]
        public void Schedule_ScoreCalculation_UsesRelevanceTable()
        {
            RelevanceTable.Clear();
            RelevanceTable.Register(ScenarioIds.Decision, "health", 0.9f);

            var scheduler = new BudgetScheduler();
            var key = MakeKey("health", ContextLayer.L2_Environment, 0.9f);

            var result = scheduler.Schedule(new List<KeyMeta> { key }, ScenarioIds.Decision, 0.5f, null);

            float expectedScore = 0.4f * 0.9f + 0.6f * 0.9f;
            Assert.Equal(expectedScore, key.CurrentScore, 3);
        }

        [Fact]
        public void Schedule_NormalizesW1W2_WhenSumExceeds1()
        {
            RelevanceTable.Clear();
            RelevanceTable.Register(ScenarioIds.Decision, "health", 0.5f);

            var scheduler = new BudgetScheduler(new BudgetSchedulerConfig { W1 = 0.8f, W2 = 0.8f });
            var key = MakeKey("health", ContextLayer.L2_Environment, 0.5f);

            scheduler.Schedule(new List<KeyMeta> { key }, ScenarioIds.Decision, 0.5f, null);

            float expectedW1 = 0.8f / 1.6f;
            float expectedW2 = 0.8f / 1.6f;
            float expectedScore = expectedW1 * 0.5f + expectedW2 * 0.5f;
            Assert.Equal(expectedScore, key.CurrentScore, 3);
        }

        [Fact]
        public void Schedule_NormalizesW1W2_L5Keys()
        {
            RelevanceTable.Clear();
            RelevanceTable.Register(ScenarioIds.Decision, "sensor1", 0.5f);

            var scheduler = new BudgetScheduler(new BudgetSchedulerConfig { W1 = 0.8f, W2 = 0.8f });
            var key = MakeKey("sensor1", ContextLayer.L5_Sensor, 0.5f);

            scheduler.Schedule(new List<KeyMeta> { key }, ScenarioIds.Decision, 0.5f, null);

            float expectedW1 = 0.8f / 1.6f;
            float expectedW2 = 0.8f / 1.6f;
            float expectedScore = expectedW1 * 0.5f + expectedW2 * 0.5f;
            Assert.Equal(expectedScore, key.CurrentScore, 3);
        }

        [Fact]
        public void Schedule_W1W2_ZeroSum_DoesNotDivideByZero()
        {
            RelevanceTable.Clear();
            RelevanceTable.Register(ScenarioIds.Decision, "health", 0.5f);

            var scheduler = new BudgetScheduler(new BudgetSchedulerConfig { W1 = 0f, W2 = 0f });
            var key = MakeKey("health", ContextLayer.L2_Environment, 0.5f);

            var result = scheduler.Schedule(new List<KeyMeta> { key }, ScenarioIds.Decision, 0.5f, null);

            Assert.Equal(0f, key.CurrentScore, 3);
        }
    }
}
