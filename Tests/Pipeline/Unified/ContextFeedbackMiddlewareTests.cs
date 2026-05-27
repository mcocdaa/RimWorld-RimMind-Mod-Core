using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Pipeline.Unified;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    /// <summary>
    /// Mock IRelevanceLearner that records calls for verification.
    /// </summary>
    internal sealed class MockRelevanceLearner : IRelevanceLearner
    {
        public List<(string TraceId, string Scenario, IReadOnlyList<string> Keys)> InclusionCalls { get; } = new();
        public List<(string TraceId, IReadOnlyList<string> Keys)> UsageCalls { get; } = new();

        private readonly Dictionary<(string Scenario, string Key), float> _scores = new();

        public void RecordInclusion(string traceId, string scenario, IReadOnlyList<string> includedKeys)
        {
            InclusionCalls.Add((traceId, scenario, includedKeys));
        }

        public void RecordUsage(string traceId, IReadOnlyList<string> usedKeys)
        {
            UsageCalls.Add((traceId, usedKeys));
        }

        public float GetFeedbackScore(string scenario, string key)
        {
            return _scores.TryGetValue((scenario, key), out var score) ? score : 0.5f;
        }

        public void SetScore(string scenario, string key, float score)
        {
            _scores[(scenario, key)] = score;
        }
    }

    public class ContextFeedbackMiddlewareTests
    {
        [Fact]
        public async Task RecordsInclusion_WhenSnapshotHasIncludedKeys()
        {
            var learner = new MockRelevanceLearner();
            var analyzer = new AIResponseAnalyzer();
            var middleware = new ContextFeedbackMiddleware(learner, analyzer);

            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.IncludedKeys = new[] { "key-a", "key-b" };

            var ctx = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    TraceId = "trace-1",
                    ScenarioId = "dialogue",
                },
                Snapshot = snapshot,
            };

            await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

            Assert.Single(learner.InclusionCalls);
            Assert.Equal("trace-1", learner.InclusionCalls[0].TraceId);
            Assert.Equal("dialogue", learner.InclusionCalls[0].Scenario);
            Assert.Equal(new[] { "key-a", "key-b" }, learner.InclusionCalls[0].Keys);
        }

        [Fact]
        public async Task RecordsUsage_WhenResultIsOk()
        {
            var learner = new MockRelevanceLearner();
            var analyzer = new AIResponseAnalyzer();
            var middleware = new ContextFeedbackMiddleware(learner, analyzer);

            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.IncludedKeys = new[] { "key-a" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "The pawn is hungry and needs to eat something soon or will starve.",
                LayerTag = "key-a"
            });

            var ctx = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    TraceId = "trace-1",
                    ScenarioId = "dialogue",
                },
                Snapshot = snapshot,
                Result = Result<LlmResponse, RimMindError>.Ok(new LlmResponse
                {
                    Content = "The pawn is hungry and needs to eat something soon or will starve. I will find food.",
                    State = AIRequestState.Completed
                }),
            };

            await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

            Assert.Single(learner.UsageCalls);
            Assert.Equal("trace-1", learner.UsageCalls[0].TraceId);
            Assert.Contains("key-a", learner.UsageCalls[0].Keys);
        }

        [Fact]
        public async Task RecordsUsageWithEmptyKeys_WhenResultIsErr()
        {
            var learner = new MockRelevanceLearner();
            var analyzer = new AIResponseAnalyzer();
            var middleware = new ContextFeedbackMiddleware(learner, analyzer);

            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.IncludedKeys = new[] { "key-a" };

            var ctx = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    TraceId = "trace-1",
                    ScenarioId = "dialogue",
                },
                Snapshot = snapshot,
                Result = Result<LlmResponse, RimMindError>.Err(RimMindErrors.Internal("test error")),
            };

            await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

            Assert.Single(learner.UsageCalls);
            Assert.Equal("trace-1", learner.UsageCalls[0].TraceId);
            Assert.Empty(learner.UsageCalls[0].Keys);
        }

        [Fact]
        public async Task DoesNotRecordUsage_WhenResultIsNull()
        {
            var learner = new MockRelevanceLearner();
            var analyzer = new AIResponseAnalyzer();
            var middleware = new ContextFeedbackMiddleware(learner, analyzer);

            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.IncludedKeys = new[] { "key-a" };

            var ctx = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    TraceId = "trace-1",
                    ScenarioId = "dialogue",
                },
                Snapshot = snapshot,
                Result = null,
            };

            await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

            Assert.Empty(learner.UsageCalls);
        }

        [Fact]
        public async Task SkipsInclusion_WhenNoIncludedKeys()
        {
            var learner = new MockRelevanceLearner();
            var analyzer = new AIResponseAnalyzer();
            var middleware = new ContextFeedbackMiddleware(learner, analyzer);

            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.IncludedKeys = new string[0];

            var ctx = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    TraceId = "trace-1",
                    ScenarioId = "dialogue",
                },
                Snapshot = snapshot,
            };

            await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

            Assert.Empty(learner.InclusionCalls);
        }

        [Fact]
        public async Task SkipsInclusion_WhenSnapshotIsNull()
        {
            var learner = new MockRelevanceLearner();
            var analyzer = new AIResponseAnalyzer();
            var middleware = new ContextFeedbackMiddleware(learner, analyzer);

            var ctx = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    TraceId = "trace-1",
                    ScenarioId = "dialogue",
                },
                Snapshot = null,
                Result = Result<LlmResponse, RimMindError>.Ok(new LlmResponse
                {
                    Content = "test",
                    State = AIRequestState.Completed
                }),
            };

            await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

            Assert.Empty(learner.InclusionCalls);
            Assert.Empty(learner.UsageCalls);
        }

        [Fact]
        public async Task NextMiddleware_IsCalled()
        {
            var learner = new MockRelevanceLearner();
            var analyzer = new AIResponseAnalyzer();
            var middleware = new ContextFeedbackMiddleware(learner, analyzer);

            var ctx = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    TraceId = "trace-1",
                    ScenarioId = "dialogue",
                },
            };

            bool nextCalled = false;
            await middleware.InvokeAsync(ctx, _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
        }
    }
}
