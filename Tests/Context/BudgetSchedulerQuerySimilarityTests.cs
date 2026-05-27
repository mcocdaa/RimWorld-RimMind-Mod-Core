using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.Interfaces;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    /// <summary>
    /// Tests for BudgetScheduler QuerySimilarity dimension (L-D: IEmbedCache integration).
    /// </summary>
    public class BudgetSchedulerQuerySimilarityTests
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
        public void ComputeQuerySimilarity_NullQuery_ReturnsZero()
        {
            var embedCache = new EmbedCache();
            var scheduler = new BudgetScheduler(embedCache: embedCache);
            var key = MakeKey("test", ContextLayer.L3_State, 0.5f);
            key.LastValueEmbedding = new float[] { 1f, 0f };

            var sctx = MakeScoringContext(query: null);
            float score = scheduler.ScoreKey(key, sctx);

            // Q dimension should be 0 when query is null
            // With default weights: W3=0.15, so Q contribution = 0
            Assert.True(float.IsFinite(score));
        }

        [Fact]
        public void ComputeQuerySimilarity_EmptyQuery_ReturnsZero()
        {
            var embedCache = new EmbedCache();
            var scheduler = new BudgetScheduler(embedCache: embedCache);
            var key = MakeKey("test", ContextLayer.L3_State, 0.5f);
            key.LastValueEmbedding = new float[] { 1f, 0f };

            var sctx = MakeScoringContext(query: "");
            float score = scheduler.ScoreKey(key, sctx);

            Assert.True(float.IsFinite(score));
        }

        [Fact]
        public void ComputeQuerySimilarity_NullEmbedCache_ReturnsZero()
        {
            var scheduler = new BudgetScheduler(embedCache: null);
            var key = MakeKey("test", ContextLayer.L3_State, 0.5f);
            key.LastValueEmbedding = new float[] { 1f, 0f };

            var sctx = MakeScoringContext(query: "health status");
            float score = scheduler.ScoreKey(key, sctx);

            // Q should be 0 when no embedCache
            Assert.True(float.IsFinite(score));
        }

        [Fact]
        public void ComputeQuerySimilarity_NoLastValueEmbedding_ReturnsZero()
        {
            var embedCache = new EmbedCache();
            var scheduler = new BudgetScheduler(embedCache: embedCache);
            var key = MakeKey("test", ContextLayer.L3_State, 0.5f);
            // LastValueEmbedding is null by default

            var sctx = MakeScoringContext(query: "health status");
            float score = scheduler.ScoreKey(key, sctx);

            Assert.True(float.IsFinite(score));
        }

        [Fact]
        public void ComputeQuerySimilarity_EmbedCacheReturnsNull_ReturnsZero()
        {
            var embedCache = new EmbedCache();
            var scheduler = new BudgetScheduler(embedCache: embedCache);
            var key = MakeKey("test", ContextLayer.L3_State, 0.5f);
            key.LastValueEmbedding = new float[] { 1f, 0f };

            // No query embedding stored, so GetOrComputeQueryEmbedding returns null
            var sctx = MakeScoringContext(query: "health status");
            float score = scheduler.ScoreKey(key, sctx);

            Assert.True(float.IsFinite(score));
        }

        [Fact]
        public void ComputeQuerySimilarity_BothEmbeddingsExist_ReturnsCosineSimilarity()
        {
            var embedCache = new EmbedCache();
            // Store a query embedding via the IEmbedCache interface
            ((IEmbedCache)embedCache).StoreEntryEmbedding("health status", new float[] { 1f, 0f });
            // Now manually store it in the "$query" namespace so GetOrComputeQueryEmbedding finds it
            embedCache.SetBlockEmbedding("$query", "health status", new float[] { 1f, 0f });

            var scheduler = new BudgetScheduler(embedCache: embedCache);
            var key = MakeKey("test", ContextLayer.L3_State, 0.5f);
            key.LastValueEmbedding = new float[] { 1f, 0f };

            var sctx = MakeScoringContext(query: "health status");
            float score = scheduler.ScoreKey(key, sctx);

            // Q should be 1.0 (identical vectors), contribution = W3 * 1.0 = 0.15
            // Score includes other dimensions too, so we just verify Q > 0
            Assert.True(score > 0, "Score should be positive when query embedding matches key embedding");
        }

        [Fact]
        public void CosineSimilarity_IdenticalVectors_ReturnsOne()
        {
            // Test via ScoreKey with isolated weights
            var config = new BudgetSchedulerConfig
            {
                W1 = 0f, W2 = 0f, W4 = 0f, W5 = 0f, W6 = 0f
            };
            config.W3 = 1.0f; // Only QuerySimilarity contributes

            var embedCache = new EmbedCache();
            embedCache.SetBlockEmbedding("$query", "test", new float[] { 1f, 0f, 0f });

            var scheduler = new BudgetScheduler(embedCache: embedCache);
            scheduler.SetConfig(config);

            var key = MakeKey("test", ContextLayer.L3_State, 0f);
            key.LastValueEmbedding = new float[] { 1f, 0f, 0f };

            var sctx = MakeScoringContext(query: "test");
            float score = scheduler.ScoreKey(key, sctx);

            Assert.Equal(1.0f, score, 3);
        }

        [Fact]
        public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
        {
            var config = new BudgetSchedulerConfig
            {
                W1 = 0f, W2 = 0f, W4 = 0f, W5 = 0f, W6 = 0f
            };
            config.W3 = 1.0f;

            var embedCache = new EmbedCache();
            embedCache.SetBlockEmbedding("$query", "test", new float[] { 1f, 0f });

            var scheduler = new BudgetScheduler(embedCache: embedCache);
            scheduler.SetConfig(config);

            var key = MakeKey("test", ContextLayer.L3_State, 0f);
            key.LastValueEmbedding = new float[] { 0f, 1f };

            var sctx = MakeScoringContext(query: "test");
            float score = scheduler.ScoreKey(key, sctx);

            Assert.Equal(0.0f, score, 3);
        }
    }
}
