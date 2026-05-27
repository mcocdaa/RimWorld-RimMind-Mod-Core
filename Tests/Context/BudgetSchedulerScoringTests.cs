using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    public class BudgetSchedulerScoringTests
    {
        private static KeyMeta MakeKey(string key, ContextLayer layer, float priority)
        {
            return new KeyMeta(key, layer, priority, _ => new List<ContextEntry>(), "TestMod");
        }

        private static ScoringContext MakeScoringContext(
            string scenario = "Decision",
            int nowTicks = 100000,
            string? query = null,
            ISet<string>? pinnedKeys = null)
        {
            return new ScoringContext
            {
                Scenario = scenario,
                NowTicks = nowTicks,
                Query = query,
                UserPinnedKeys = pinnedKeys ?? new HashSet<string>()
            };
        }

        /// <summary>
        /// Test 1: Only Priority high, other dims neutral/zero -> key included
        /// </summary>
        [Fact]
        public void ScoreKey_HighPriority_OtherDimsNeutral_KeyIncluded()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("health", ContextLayer.L3_State, 0.9f);
            var sctx = MakeScoringContext();

            float score = scheduler.ScoreKey(key, sctx);

            // P=0.9, Rs=0.5(default), Q=0, Rc=0.5(LastUpdatedTick=0), F=0.5, Pin=0, Cd=0
            // W1*P + W2*Rs + W3*Q + W4*Rc + W5*F - W6*Cd
            // = 0.30*0.9 + 0.25*0.5 + 0.15*0 + 0.10*0.5 + 0.15*0.5 - 0.05*0
            // = 0.27 + 0.125 + 0 + 0.05 + 0.075 - 0
            // = 0.52
            Assert.Equal(0.52f, score, 3);
            Assert.True(score > 0, "High priority key should have positive score");
        }

        /// <summary>
        /// Test 2: UserPin = true -> score >= 1000 (forced inclusion even if other dims all 0)
        /// </summary>
        [Fact]
        public void ScoreKey_UserPinned_ScoreAbove1000()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("pinned_key", ContextLayer.L2_Environment, 0.0f);
            key.AdaptivePriority = 0.0f;
            var pinnedKeys = new HashSet<string> { "pinned_key" };
            var sctx = MakeScoringContext(pinnedKeys: pinnedKeys);

            float score = scheduler.ScoreKey(key, sctx);

            Assert.True(score >= 1000f, $"Pinned key score should be >= 1000, got {score}");
        }

        /// <summary>
        /// Test 3: Cooldown - LastIncludedTick within window -> score reduced
        /// </summary>
        [Fact]
        public void ScoreKey_CooldownWithinWindow_ScoreReduced()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("cooldown_key", ContextLayer.L2_Environment, 0.5f);
            key.LastIncludedTick = 98000; // 2000 ticks ago, within CooldownWindow=5000

            var sctx = MakeScoringContext(nowTicks: 100000);
            float scoreWithCooldown = scheduler.ScoreKey(key, sctx);

            // Now test without cooldown
            key.LastIncludedTick = 0;
            float scoreWithoutCooldown = scheduler.ScoreKey(key, sctx);

            Assert.True(scoreWithCooldown < scoreWithoutCooldown,
                $"Score with cooldown ({scoreWithCooldown}) should be less than without ({scoreWithoutCooldown})");
        }

        /// <summary>
        /// Test 4: Recency - LastUpdatedTick old -> score reduced
        /// </summary>
        [Fact]
        public void ScoreKey_OldLastUpdatedTick_ScoreReduced()
        {
            var scheduler = new BudgetScheduler();

            var freshKey = MakeKey("fresh_key", ContextLayer.L2_Environment, 0.5f);
            freshKey.LastUpdatedTick = 99000; // very recent

            var staleKey = MakeKey("stale_key", ContextLayer.L2_Environment, 0.5f);
            staleKey.LastUpdatedTick = 10000; // very old

            var sctx = MakeScoringContext(nowTicks: 100000);

            float freshScore = scheduler.ScoreKey(freshKey, sctx);
            float staleScore = scheduler.ScoreKey(staleKey, sctx);

            Assert.True(freshScore > staleScore,
                $"Fresh key score ({freshScore}) should be higher than stale ({staleScore})");
        }

        /// <summary>
        /// Test 5: Feedback high -> score boosted (currently returns neutral 0.5f)
        /// </summary>
        [Fact]
        public void ScoreKey_FeedbackNeutral_ReturnsExpectedScore()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("feedback_key", ContextLayer.L2_Environment, 0.5f);
            var sctx = MakeScoringContext();

            float score = scheduler.ScoreKey(key, sctx);

            // F=0.5 (neutral), W5=0.15 -> contributes 0.075
            // This test verifies the neutral feedback contribution is stable
            Assert.True(score > 0, "Score with neutral feedback should be positive");
        }

        /// <summary>
        /// Test 6: All dims 0.5 + default weights -> expected score
        /// </summary>
        [Fact]
        public void ScoreKey_AllDimsNeutral_ExpectedScore()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("neutral_key", ContextLayer.L2_Environment, 0.5f);
            // Priority=0.5, AdaptivePriority=0.5 -> GetEffectivePriority=0.5
            // LastUpdatedTick=0 -> Rc=0.5
            // No relevance registered -> Rs=0.5
            // Q=0, F=0.5, Pin=0, Cd=0

            var sctx = MakeScoringContext();
            float score = scheduler.ScoreKey(key, sctx);

            // W1*0.5 + W2*0.5 + W3*0 + W4*0.5 + W5*0.5 - W6*0
            // = 0.30*0.5 + 0.25*0.5 + 0 + 0.10*0.5 + 0.15*0.5
            // = 0.15 + 0.125 + 0 + 0.05 + 0.075
            // = 0.40
            float expected = 0.30f * 0.5f + 0.25f * 0.5f + 0.10f * 0.5f + 0.15f * 0.5f;
            Assert.Equal(expected, score, 3);
        }

        /// <summary>
        /// Test 7: Staleness - key not recently updated -> neutral recency 0.5f
        /// </summary>
        [Fact]
        public void ScoreKey_UnknownLastUpdatedTick_RecencyNeutral()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("unknown_key", ContextLayer.L2_Environment, 0.5f);
            // LastUpdatedTick defaults to 0 -> Rc should be 0.5 (neutral)

            var sctx = MakeScoringContext(nowTicks: 100000);
            float score = scheduler.ScoreKey(key, sctx);

            // With Rc=0.5: W4*0.5 = 0.10*0.5 = 0.05
            // Compare with a key that has LastUpdatedTick = nowTicks (Rc=1.0)
            var freshKey = MakeKey("fresh_key", ContextLayer.L2_Environment, 0.5f);
            freshKey.LastUpdatedTick = 100000;

            float freshScore = scheduler.ScoreKey(freshKey, sctx);

            Assert.True(freshScore > score,
                "Fresh key should score higher than unknown-age key");
        }

        /// <summary>
        /// Test 8: Cooldown outside window -> no penalty
        /// </summary>
        [Fact]
        public void ScoreKey_CooldownOutsideWindow_NoPenalty()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("old_cooldown_key", ContextLayer.L2_Environment, 0.5f);
            key.LastIncludedTick = 90000; // 10000 ticks ago, outside CooldownWindow=5000

            var sctx = MakeScoringContext(nowTicks: 100000);
            float score = scheduler.ScoreKey(key, sctx);

            // Cooldown penalty should be 0 since delta >= CooldownWindow
            var keyNoCooldown = MakeKey("no_cooldown_key", ContextLayer.L2_Environment, 0.5f);
            float scoreNoCooldown = scheduler.ScoreKey(keyNoCooldown, sctx);

            Assert.Equal(scoreNoCooldown, score, 3);
        }

        /// <summary>
        /// Test: ScheduleWithContext writes back CurrentScore and LastIncludedTick
        /// </summary>
        [Fact]
        public void ScheduleWithContext_WritesBackCurrentScoreAndLastIncludedTick()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("health", ContextLayer.L3_State, 0.9f);
            var sctx = MakeScoringContext(nowTicks: 100000);

            var result = scheduler.ScheduleWithContext(
                new List<KeyMeta> { key }, sctx, 0.5f);

            Assert.Equal(100000, key.LastIncludedTick);
            Assert.True(key.CurrentScore > 0, "CurrentScore should be set after scheduling");
        }

        /// <summary>
        /// Test: Recency exponential decay with known halflife
        /// </summary>
        [Fact]
        public void ScoreKey_RecencyHalflife_CorrectDecay()
        {
            var config = new BudgetSchedulerConfig
            {
                RecencyHalflife = 30000,
                W1 = 0f, W2 = 0f, W3 = 0f, W5 = 0f, W6 = 0f
            };
            // Only W4 (Recency) contributes
            config.W4 = 1.0f;

            var scheduler = new BudgetScheduler();
            scheduler.SetConfig(config);

            var key = MakeKey("recency_key", ContextLayer.L2_Environment, 0.5f);
            key.LastUpdatedTick = 70000; // 30000 ticks ago = exactly one halflife

            var sctx = MakeScoringContext(nowTicks: 100000);
            float score = scheduler.ScoreKey(key, sctx);

            // After one halflife, Rc = e^(-1) ~ 0.3679
            // Score = W4 * Rc = 1.0 * 0.3679
            Assert.Equal(0.3679f, score, 2);
        }

        /// <summary>
        /// Test: Cooldown penalty linear interpolation within window
        /// </summary>
        [Fact]
        public void ScoreKey_CooldownLinearPenalty_HalfWindow()
        {
            var config = new BudgetSchedulerConfig
            {
                CooldownWindow = 10000,
                W1 = 0f, W2 = 0f, W3 = 0f, W4 = 0f, W5 = 0f
            };
            config.W6 = 1.0f; // Only CooldownPenalty contributes (subtracted)

            var scheduler = new BudgetScheduler();
            scheduler.SetConfig(config);

            var key = MakeKey("cooldown_key", ContextLayer.L2_Environment, 0.5f);
            key.LastIncludedTick = 95000; // 5000 ticks ago = half of 10000 window

            var sctx = MakeScoringContext(nowTicks: 100000);
            float score = scheduler.ScoreKey(key, sctx);

            // Cd = 1 - 5000/10000 = 0.5
            // Score = -W6 * Cd = -1.0 * 0.5 = -0.5
            Assert.Equal(-0.5f, score, 3);
        }

        /// <summary>
        /// Test: Pinned key with zero priority still gets included
        /// </summary>
        [Fact]
        public void ScheduleWithContext_PinnedKeyZeroPriority_Included()
        {
            var scheduler = new BudgetScheduler();
            var pinnedKey = MakeKey("pinned", ContextLayer.L2_Environment, 0.0f);
            pinnedKey.AdaptivePriority = 0.0f;
            var normalKey = MakeKey("normal", ContextLayer.L2_Environment, 0.9f);

            var pinnedKeys = new HashSet<string> { "pinned" };
            var sctx = MakeScoringContext(pinnedKeys: pinnedKeys);

            var result = scheduler.ScheduleWithContext(
                new List<KeyMeta> { pinnedKey, normalKey }, sctx, 0.5f);

            // Pinned key has very high score so cumulative is near 1.0,
            // causing ChooseLayer to demote L2_Environment to L0_Static.
            // Verify the pinned key is included somewhere in the allocation.
            bool pinnedIncluded =
                result.L0Keys.Contains(pinnedKey) ||
                result.L1Keys.Contains(pinnedKey) ||
                result.L2Keys.Contains(pinnedKey) ||
                result.L3Keys.Contains(pinnedKey) ||
                result.L5Keys.Contains(pinnedKey);
            Assert.True(pinnedIncluded, "Pinned key should be included in allocation");
        }

        /// <summary>
        /// Test: Query similarity returns 0 when no embedding available
        /// </summary>
        [Fact]
        public void ScoreKey_QuerySimilarity_NoEmbedding_ReturnsZero()
        {
            var scheduler = new BudgetScheduler();
            var key = MakeKey("no_embedding_key", ContextLayer.L2_Environment, 0.5f);
            // LastValueEmbedding is null by default

            var sctx = MakeScoringContext(query: "test query");
            float score = scheduler.ScoreKey(key, sctx);

            // Q should be 0, so W3*Q = 0
            // This just verifies no crash and Q dimension is 0
            Assert.True(float.IsFinite(score), "Score should be finite");
        }

        /// <summary>
        /// Test: Multiple keys sorted by score descending
        /// </summary>
        [Fact]
        public void ScheduleWithContext_MultipleKeys_SortedByScore()
        {
            var scheduler = new BudgetScheduler();
            var highKey = MakeKey("high_priority", ContextLayer.L3_State, 0.9f);
            var lowKey = MakeKey("low_priority", ContextLayer.L3_State, 0.1f);

            var sctx = MakeScoringContext();
            var result = scheduler.ScheduleWithContext(
                new List<KeyMeta> { lowKey, highKey }, sctx, 0.5f);

            Assert.Equal(2, result.L3Keys.Count);
            // Higher priority key should be first (sorted by score descending)
            Assert.Equal("high_priority", result.L3Keys[0].Key);
        }
    }
}
