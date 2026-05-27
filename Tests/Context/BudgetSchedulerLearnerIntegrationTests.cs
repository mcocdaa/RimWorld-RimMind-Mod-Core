using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    /// <summary>
    /// Tests for N3.1: IRelevanceLearner -> BudgetScheduler connection.
    /// Verifies that BudgetScheduler delegates GetFeedbackScore to IRelevanceLearner.
    /// </summary>
    public class BudgetSchedulerLearnerIntegrationTests
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

        [Fact]
        public void ScoreKey_WithLearner_UsesLearnerFeedbackScore()
        {
            var learner = new RelevanceLearner();
            // Record that "health" key was used 100% of the time in "Decision" scenario
            learner.RecordInclusion("t1", "Decision", new[] { "health" });
            learner.RecordUsage("t1", new[] { "health" });

            var scheduler = new BudgetScheduler(learner: learner);
            var key = MakeKey("health", ContextLayer.L3_State, 0.5f);
            var sctx = MakeScoringContext();

            float score = scheduler.ScoreKey(key, sctx);

            // With learner returning 1.0 for feedback, the score should be higher
            // than the neutral 0.5f feedback case
            var schedulerNoLearner = new BudgetScheduler();
            float neutralScore = schedulerNoLearner.ScoreKey(key, sctx);

            Assert.True(score > neutralScore,
                $"Score with learner feedback (F=1.0) should be higher than neutral (F=0.5). " +
                $"Got {score} vs {neutralScore}");
        }

        [Fact]
        public void ScoreKey_WithLearner_LowFeedback_DecreasesScore()
        {
            var learner = new RelevanceLearner();
            // Record that "mood" key was never used in "Decision" scenario
            learner.RecordInclusion("t1", "Decision", new[] { "mood" });
            learner.RecordUsage("t1", new string[0]);

            var scheduler = new BudgetScheduler(learner: learner);
            var key = MakeKey("mood", ContextLayer.L3_State, 0.5f);
            var sctx = MakeScoringContext();

            float score = scheduler.ScoreKey(key, sctx);

            // With learner returning 0.0 for feedback, the score should be lower
            // than the neutral 0.5f feedback case
            var schedulerNoLearner = new BudgetScheduler();
            float neutralScore = schedulerNoLearner.ScoreKey(key, sctx);

            Assert.True(score < neutralScore,
                $"Score with learner feedback (F=0.0) should be lower than neutral (F=0.5). " +
                $"Got {score} vs {neutralScore}");
        }

        [Fact]
        public void ScoreKey_NullLearner_ReturnsNeutralFeedback()
        {
            // No learner passed - should default to 0.5f
            var scheduler = new BudgetScheduler(learner: null);
            var key = MakeKey("test_key", ContextLayer.L2_Environment, 0.5f);
            var sctx = MakeScoringContext();

            float score = scheduler.ScoreKey(key, sctx);

            // With W5=0.15 and F=0.5, contribution is 0.075
            // P=0.5, Rs=0.5, Q=0, Rc=0.5, F=0.5, Pin=0, Cd=0
            // = 0.30*0.5 + 0.25*0.5 + 0.10*0.5 + 0.15*0.5
            // = 0.15 + 0.125 + 0.05 + 0.075 = 0.40
            float expected = 0.30f * 0.5f + 0.25f * 0.5f + 0.10f * 0.5f + 0.15f * 0.5f;
            Assert.Equal(expected, score, 3);
        }

        [Fact]
        public void ScoreKey_LearnerUnknownKey_ReturnsNeutralFeedback()
        {
            var learner = new RelevanceLearner();
            // No data recorded for this key/scenario

            var scheduler = new BudgetScheduler(learner: learner);
            var key = MakeKey("unknown_key", ContextLayer.L2_Environment, 0.5f);
            var sctx = MakeScoringContext();

            float score = scheduler.ScoreKey(key, sctx);

            // Unknown key should return neutral 0.5f from learner
            float expected = 0.30f * 0.5f + 0.25f * 0.5f + 0.10f * 0.5f + 0.15f * 0.5f;
            Assert.Equal(expected, score, 3);
        }

        [Fact]
        public void ScoreKey_LearnerDifferentScenario_Isolated()
        {
            var learner = new RelevanceLearner();
            // Record high usage in "Dialogue" scenario
            learner.RecordInclusion("t1", "Dialogue", new[] { "health" });
            learner.RecordUsage("t1", new[] { "health" });

            var scheduler = new BudgetScheduler(learner: learner);
            var key = MakeKey("health", ContextLayer.L3_State, 0.5f);

            // Query "Decision" scenario - should get neutral feedback since
            // data was recorded for "Dialogue"
            var sctx = MakeScoringContext(scenario: "Decision");
            float decisionScore = scheduler.ScoreKey(key, sctx);

            // Query "Dialogue" scenario - should get high feedback
            var sctxDialogue = MakeScoringContext(scenario: "Dialogue");
            float dialogueScore = scheduler.ScoreKey(key, sctxDialogue);

            Assert.True(dialogueScore > decisionScore,
                $"Dialogue score should be higher than Decision score due to feedback isolation. " +
                $"Got Dialogue={dialogueScore} vs Decision={decisionScore}");
        }

        [Fact]
        public void Constructor_DefaultParameters_CreatesSuccessfully()
        {
            // Verify backward compatibility - both params optional
            var scheduler1 = new BudgetScheduler();
            var scheduler2 = new BudgetScheduler(relevanceTable: null);
            var scheduler3 = new BudgetScheduler(learner: null);
            var scheduler4 = new BudgetScheduler(relevanceTable: null, learner: null);

            Assert.NotNull(scheduler1);
            Assert.NotNull(scheduler2);
            Assert.NotNull(scheduler3);
            Assert.NotNull(scheduler4);
        }

        [Fact]
        public void Schedule_WithLearner_ProducesDifferentRanking()
        {
            var learner = new RelevanceLearner();
            // "health" has high feedback (always used)
            learner.RecordInclusion("t1", "Decision", new[] { "health", "mood" });
            learner.RecordUsage("t1", new[] { "health" }); // only health used

            var scheduler = new BudgetScheduler(learner: learner);
            var healthKey = MakeKey("health", ContextLayer.L3_State, 0.5f);
            var moodKey = MakeKey("mood", ContextLayer.L3_State, 0.5f);

            var result = scheduler.ScheduleWithContext(
                new List<KeyMeta> { moodKey, healthKey },
                new ScoringContext
                {
                    Scenario = "Decision",
                    NowTicks = 100000,
                    Query = null,
                    UserPinnedKeys = new HashSet<string>()
                },
                0.5f);

            // health should rank higher than mood due to feedback
            Assert.Equal(2, result.L3Keys.Count);
            Assert.Equal("health", result.L3Keys[0].Key);
        }
    }
}
