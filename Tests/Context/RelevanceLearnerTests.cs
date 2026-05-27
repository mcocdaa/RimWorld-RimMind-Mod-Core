using RimMind.Application.Features.Context;
using Xunit;

namespace RimMind.Tests.Context
{
    public class RelevanceLearnerTests
    {
        [Fact]
        public void RecordInclusionAndUsage_ProducesCorrectScore()
        {
            var learner = new RelevanceLearner();

            // Record inclusion of 3 keys
            learner.RecordInclusion("trace-1", "dialogue", new[] { "key-a", "key-b", "key-c" });

            // Record that only key-a was used
            learner.RecordUsage("trace-1", new[] { "key-a" });

            // key-a should have score 1.0 (used 1 out of 1)
            Assert.Equal(1.0f, learner.GetFeedbackScore("dialogue", "key-a"), precision: 2);

            // key-b should have score 0.0 (used 0 out of 1)
            Assert.Equal(0.0f, learner.GetFeedbackScore("dialogue", "key-b"), precision: 2);

            // key-c should have score 0.0
            Assert.Equal(0.0f, learner.GetFeedbackScore("dialogue", "key-c"), precision: 2);
        }

        [Fact]
        public void UnknownKey_ReturnsNeutralScore()
        {
            var learner = new RelevanceLearner();
            Assert.Equal(0.5f, learner.GetFeedbackScore("unknown-scenario", "unknown-key"));
        }

        [Fact]
        public void RecordUsage_WithNoMatchingTrace_IgnoresGracefully()
        {
            var learner = new RelevanceLearner();
            // No prior RecordInclusion for this traceId
            learner.RecordUsage("trace-ghost", new[] { "key-a" });

            // Should not throw, and unknown key should still return neutral
            Assert.Equal(0.5f, learner.GetFeedbackScore("any", "key-a"));
        }

        [Fact]
        public void SameTraceId_SecondRecordUsage_Ignores()
        {
            var learner = new RelevanceLearner();
            learner.RecordInclusion("trace-1", "scenario", new[] { "key-a" });

            // First usage consumes the pending trace
            learner.RecordUsage("trace-1", new[] { "key-a" });
            Assert.Equal(1.0f, learner.GetFeedbackScore("scenario", "key-a"), precision: 2);

            // Second usage with same traceId should be ignored (already consumed)
            learner.RecordUsage("trace-1", new[] { "key-a" });
            // Score should still be 1.0 (1 out of 1), not 2 out of 2
            Assert.Equal(1.0f, learner.GetFeedbackScore("scenario", "key-a"), precision: 2);
        }

        [Fact]
        public void MultipleTraces_AccumulateCorrectly()
        {
            var learner = new RelevanceLearner();

            // Trace 1: key-a used
            learner.RecordInclusion("t1", "scenario", new[] { "key-a" });
            learner.RecordUsage("t1", new[] { "key-a" });

            // Trace 2: key-a not used
            learner.RecordInclusion("t2", "scenario", new[] { "key-a" });
            learner.RecordUsage("t2", new string[0]);

            // Trace 3: key-a used
            learner.RecordInclusion("t3", "scenario", new[] { "key-a" });
            learner.RecordUsage("t3", new[] { "key-a" });

            // Score should be 2/3
            Assert.Equal(2f / 3f, learner.GetFeedbackScore("scenario", "key-a"), precision: 2);
        }

        [Fact]
        public void RecordInclusion_NullTraceId_Ignores()
        {
            var learner = new RelevanceLearner();
            learner.RecordInclusion(null!, "scenario", new[] { "key-a" });
            learner.RecordInclusion("", "scenario", new[] { "key-a" });

            // No pending traces should exist
            learner.RecordUsage("nonexistent", new[] { "key-a" });
            Assert.Equal(0.5f, learner.GetFeedbackScore("scenario", "key-a"));
        }

        [Fact]
        public void RecordUsage_NullTraceId_Ignores()
        {
            var learner = new RelevanceLearner();
            learner.RecordInclusion("trace-1", "scenario", new[] { "key-a" });

            // Null/empty traceId should not consume the pending trace
            learner.RecordUsage(null!, new[] { "key-a" });
            learner.RecordUsage("", new[] { "key-a" });

            // Original trace should still be consumable
            learner.RecordUsage("trace-1", new[] { "key-a" });
            Assert.Equal(1.0f, learner.GetFeedbackScore("scenario", "key-a"), precision: 2);
        }

        [Fact]
        public void RingBufferFullReplacement_OldestDataDropped()
        {
            var learner = new RelevanceLearner();

            // Fill 100 entries (ring size) where key-a is always used
            for (int i = 0; i < 100; i++)
            {
                var traceId = $"trace-{i}";
                learner.RecordInclusion(traceId, "scenario", new[] { "key-a" });
                learner.RecordUsage(traceId, new[] { "key-a" });
            }

            // Score should be 1.0
            Assert.Equal(1.0f, learner.GetFeedbackScore("scenario", "key-a"), precision: 2);

            // Now add 100 more entries where key-a is never used
            for (int i = 100; i < 200; i++)
            {
                var traceId = $"trace-{i}";
                learner.RecordInclusion(traceId, "scenario", new[] { "key-a" });
                learner.RecordUsage(traceId, new string[0]);
            }

            // Score should now be 0.0 (all old "used" entries were pushed out of the ring)
            Assert.Equal(0.0f, learner.GetFeedbackScore("scenario", "key-a"), precision: 2);
        }

        [Fact]
        public void DifferentScenarios_AreIsolated()
        {
            var learner = new RelevanceLearner();

            learner.RecordInclusion("t1", "scenario-a", new[] { "key-x" });
            learner.RecordUsage("t1", new[] { "key-x" });

            learner.RecordInclusion("t2", "scenario-b", new[] { "key-x" });
            learner.RecordUsage("t2", new string[0]);

            Assert.Equal(1.0f, learner.GetFeedbackScore("scenario-a", "key-x"), precision: 2);
            Assert.Equal(0.0f, learner.GetFeedbackScore("scenario-b", "key-x"), precision: 2);
        }
    }
}
