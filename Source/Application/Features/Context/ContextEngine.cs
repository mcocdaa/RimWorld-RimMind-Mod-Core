using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    public sealed class ContextEngine : IContextEngine
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

        public object? Orchestrator { get; set; }

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

        public ContextEngine(IHistoryManager historyManager)
        {
            _historyManager = historyManager;
            _cache = new DefaultContextCacheManager();
            _diff = new DefaultContextDiffTracker();
            _layerBuilder = new DefaultContextLayerBuilder();
            _keyProvider = new DefaultContextKeyProvider();
            _scheduler = new DefaultBudgetScheduler();
            _embeddingStore = new EmbeddingSnapshotStore();
            _log = null;
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

    internal sealed class DefaultContextCacheManager : IContextCacheManager
    {
        public int GetL0CacheCount() => 0;
        public int GetL1BlockCacheCount() => 0;
        public int GetEmbedCacheCount() => 0;
        public void Reset() { }
        public void TouchCache(string cacheKey) { }
        public void RemoveL0CacheForNpc(string npcId) { }
        public void InvalidateLayer(string npcId, ContextLayer layer) { }
        public void InvalidateKey(string npcId, string key) { }
        public void UpdateBaseline(string npcId) { }
        public void InvalidateNpc(string npcId) { }
    }

    internal sealed class DefaultContextDiffTracker : IContextDiffTracker
    {
        public int GetDiffStoreCount() => 0;
        public void Reset() { }
    }

    internal sealed class DefaultContextLayerBuilder : IContextLayerBuilder
    {
    }

    internal sealed class DefaultContextKeyProvider : IContextKeyProvider
    {
        public List<ContextEntry> BuildMapContextEntries(object map) => new List<ContextEntry>();
        public string ExtractPawnBaseInfo(object pawn) => "";
        public string ExtractFixedRelations(object pawn) => "";
        public string ExtractIdeology(object pawn) => "";
        public string ExtractSkillsSummary(object pawn) => "";
        public string ExtractCurrentArea(object pawn) => "";
        public string ExtractWeather(object pawn) => "";
        public string ExtractTimeOfDay(object pawn) => "";
        public string ExtractNearbyPawns(object pawn) => "";
        public string ExtractSeason(object pawn) => "";
        public string ExtractColonyStatus(object pawn) => "";
        public string ExtractHealth(object pawn) => "";
        public string ExtractMood(object pawn) => "";
        public string ExtractCurrentJob(object pawn) => "";
        public string ExtractCombatStatus(object pawn) => "";
        public string ExtractTargetInfo(object pawn) => "";
        public string ExtractTaskProgress(object pawn) => "";
    }

    internal sealed class DefaultBudgetScheduler : IBudgetScheduler
    {
        public BudgetAllocation Schedule(List<KeyMeta> keys, string scenarioId, float budget, string? currentQuery)
            => new BudgetAllocation();
        public void OnKeyUpdated(KeyMeta key) { }
        public void Calibrate(List<KeyMeta> keys) { }
        public void SetRelevanceProvider(IRelevanceProvider provider) { }
        public void SetConfig(BudgetSchedulerConfig? config) { }
        public BudgetSchedulerConfig GetConfig() => new BudgetSchedulerConfig();
    }
}
