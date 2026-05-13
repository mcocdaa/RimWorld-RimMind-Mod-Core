using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal sealed class ContextEngine : IContextEngine
    {
        private readonly IContextCacheManager _cache;
        private readonly IContextDiffTracker _diff;
        private readonly IContextLayerBuilder _layerBuilder;
        private readonly IContextKeyProvider _keyProvider;
        private readonly IHistoryManager _historyManager;
        private readonly IBudgetScheduler _scheduler;
        private readonly EmbeddingSnapshotStore _embeddingStore;
        private readonly ILogSink? _log;
        private bool _disposed;

        public ContextEngine(
            IContextCacheManager cache,
            IContextDiffTracker diff,
            IContextLayerBuilder layerBuilder,
            IContextKeyProvider keyProvider,
            IHistoryManager historyManager,
            IBudgetScheduler scheduler,
            EmbeddingSnapshotStore embeddingStore,
            ILogSink? log = null)
        {
            _cache = cache;
            _diff = diff;
            _layerBuilder = layerBuilder;
            _keyProvider = keyProvider;
            _historyManager = historyManager;
            _scheduler = scheduler;
            _embeddingStore = embeddingStore;
            _log = log;
        }

        public ContextSnapshot? BuildSnapshot(ContextRequest request)
        {
            if (_disposed) return null;
            var snapshot = new ContextSnapshot
            {
                NpcId = request.NpcId,
                Scenario = request.Scenario,
                BudgetValue = request.Budget,
                CurrentQuery = request.CurrentQuery,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                BuildStartTicks = DateTime.UtcNow.Ticks
            };
            return snapshot;
        }

        public int GetL0CacheCount() => _cache.GetL0CacheCount();
        public int GetL1BlockCacheCount() => _cache.GetL1BlockCacheCount();
        public int GetDiffStoreCount() => _diff.GetDiffStoreCount();
        public int GetEmbedCacheCount() => _cache.GetEmbedCacheCount();
        public void ResetCaches() { _cache.Reset(); _diff.Reset(); _embeddingStore.Clear(); }
        public void TouchCache(string cacheKey) => _cache.TouchCache(cacheKey);
        public void RemoveL0CacheForNpc(string npcId) => _cache.RemoveL0CacheForNpc(npcId);
        public void InvalidateLayer(string npcId, ContextLayer layer) => _cache.InvalidateLayer(npcId, layer);
        public void InvalidateKey(string npcId, string key) => _cache.InvalidateKey(npcId, key);
        public void UpdateBaseline(string npcId) => _cache.UpdateBaseline(npcId);
        public void InvalidateNpc(string npcId) => _cache.InvalidateNpc(npcId);
        public IBudgetScheduler? GetScheduler() => _scheduler;
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => _embeddingStore;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
