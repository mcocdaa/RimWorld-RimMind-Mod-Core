using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    internal sealed class StubContextEngine : IContextEngine
    {
        public ContextSnapshot? SnapshotResult { get; set; }

        public ContextSnapshot? BuildSnapshotFromEnvelope(string npcId, string? currentQuery, int maxTokens = 800, float temperature = 0.7f, string? scenarioId = null)
            => SnapshotResult;

        public Task<ContextSnapshot?> BuildSnapshotFromEnvelopeAsync(string npcId, string? currentQuery,
            int maxTokens = 800, float temperature = 0.7f, string? scenarioId = null,
            CancellationToken ct = default)
            => Task.FromResult(SnapshotResult);

        // IContextBuilder
        public IBudgetScheduler? GetScheduler() => null;
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => null;

        // IContextCache
        public int GetL0CacheCount() => 0;
        public int GetL1BlockCacheCount() => 0;
        public int GetDiffStoreCount() => 0;
        public int GetEmbedCacheCount() => 0;
        public void ResetCaches() { }
        public void TouchCache(string cacheKey) { }

        // IContextInvalidation
        public void RemoveL0CacheForNpc(string npcId) { }
        public void InvalidateLayer(string npcId, ContextLayer layer) { }
        public void InvalidateKey(string npcId, string key) { }
        public void UpdateBaseline(string npcId) { }
        public void InvalidateNpc(string npcId) { }

        // IDisposable
        public void Dispose() { }
    }

    public class ContextBuildMiddlewareTests
    {
        [Fact]
        public async Task MessagesAlreadyPopulated_SkipsContextBuild()
        {
            var middleware = new ContextBuildMiddleware();
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "user", Content = "hello" },
                    },
                },
            };
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
            Assert.False(context.IsShortCircuited);
        }

        [Fact]
        public async Task NoContextEngine_SkipsContextBuild()
        {
            var middleware = new ContextBuildMiddleware(contextEngine: null);
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    NpcId = "npc-1",
                },
            };
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
            Assert.False(context.IsShortCircuited);
        }

        [Fact]
        public async Task NullSnapshot_ShortCircuits()
        {
            var engine = new StubContextEngine { SnapshotResult = null };
            var middleware = new ContextBuildMiddleware(contextEngine: engine);
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    NpcId = "npc-1",
                },
            };

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("context_build_null", context.ShortCircuitReason);
        }

        [Fact]
        public async Task ValidSnapshot_PopulatesMessages()
        {
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage { Role = "system", Content = "You are helpful" });
            snapshot.AddMessage(new ChatMessage { Role = "user", Content = "hi" });
            var engine = new StubContextEngine { SnapshotResult = snapshot };
            var middleware = new ContextBuildMiddleware(contextEngine: engine);
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    NpcId = "npc-1",
                },
            };

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.Equal(2, context.Envelope.Messages.Count);
            Assert.Equal("system", context.Envelope.Messages[0].Role);
            Assert.Equal("user", context.Envelope.Messages[1].Role);
            Assert.Same(snapshot, context.Snapshot);
        }
    }
}
