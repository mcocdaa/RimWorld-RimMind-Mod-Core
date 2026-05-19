using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Client;
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
        public IReadOnlyDictionary<string, ChatMessage> L0Cache => _l0Cache;
        public IReadOnlyDictionary<string, Dictionary<string, string>> L1BlockCache => _l1BlockCache;
        public IReadOnlyDictionary<string, int> L1Version => _l1Version;
        public IReadOnlyDictionary<string, Dictionary<string, int>> L1KeyVersions => _l1KeyVersions;
        public IReadOnlyDictionary<string, bool> PendingCacheEvents => _pendingCacheEvents;
        public EmbedCache EmbedCache => _embedCache;

        private readonly Dictionary<string, ChatMessage> _l0Cache = new();
        private readonly Dictionary<string, Dictionary<string, string>> _l1BlockCache = new();
        private readonly Dictionary<string, int> _l1Version = new();
        private readonly Dictionary<string, Dictionary<string, int>> _l1KeyVersions = new();
        private readonly Dictionary<string, bool> _pendingCacheEvents = new();
        private readonly EmbedCache _embedCache = new();

        public int GetL0CacheCount() => _l0Cache.Count;
        public int GetL1BlockCacheCount() => _l1BlockCache.Count;
        public int GetEmbedCacheCount() => 0;
        public void Reset() { _l0Cache.Clear(); _l1BlockCache.Clear(); _l1Version.Clear(); _l1KeyVersions.Clear(); _pendingCacheEvents.Clear(); }
        public void TouchCache(string cacheKey) { }
        public void RemoveL0CacheForNpc(string npcId) { _l0Cache.Remove(npcId); }
        public void InvalidateLayer(string npcId, ContextLayer layer) { }
        public void InvalidateKey(string npcId, string key) { }
        public void UpdateBaseline(string npcId) { }
        public void InvalidateNpc(string npcId) { _l0Cache.Remove(npcId); _l1BlockCache.Remove(npcId); }
        public void ClearPendingCacheEvents() { _pendingCacheEvents.Clear(); }
        public bool TryGetL0CacheItem(string key, out ChatMessage msg) { return _l0Cache.TryGetValue(key, out msg!); }
        public void SetL0CacheItem(string key, ChatMessage msg) { _l0Cache[key] = msg; }
        public bool RemoveL0CacheItem(string key) { return _l0Cache.Remove(key); }
        public bool TryGetL1BlockCache(string npcId, out Dictionary<string, string> blocks) { return _l1BlockCache.TryGetValue(npcId, out blocks!); }
        public void SetL1BlockCache(string npcId, Dictionary<string, string> blocks) { _l1BlockCache[npcId] = blocks; }
        public bool TryGetL1Version(string npcId, out int version) { return _l1Version.TryGetValue(npcId, out version); }
        public void SetL1Version(string npcId, int version) { _l1Version[npcId] = version; }
        public bool TryGetL1KeyVersions(string npcId, out Dictionary<string, int> versions) { return _l1KeyVersions.TryGetValue(npcId, out versions!); }
        public void SetL1KeyVersions(string npcId, Dictionary<string, int> versions) { _l1KeyVersions[npcId] = versions; }
        public bool TryGetPendingCacheEvent(string key, out bool value) { return _pendingCacheEvents.TryGetValue(key, out value); }
        public void SetPendingCacheEvent(string key, bool value) { _pendingCacheEvents[key] = value; }
    }

    internal sealed class DefaultContextDiffTracker : IContextDiffTracker
    {
        public IReadOnlyDictionary<string, List<ContextDiff>> DiffStore => _diffStore;
        public IReadOnlyDictionary<string, Dictionary<string, string>> KeyLastValues => _keyLastValues;
        public IReadOnlyDictionary<string, Dictionary<string, float>> KeyLastNumericValues => _keyLastNumericValues;

        private readonly Dictionary<string, List<ContextDiff>> _diffStore = new();
        private readonly Dictionary<string, Dictionary<string, string>> _keyLastValues = new();
        private readonly Dictionary<string, Dictionary<string, float>> _keyLastNumericValues = new();

        public int GetDiffStoreCount() => _diffStore.Count;
        public void Reset() { _diffStore.Clear(); _keyLastValues.Clear(); _keyLastNumericValues.Clear(); }
        public void AddDiff(string npcId, string key, string oldValue, string newValue, ContextLayer layer) { }
        public void MergeExpiredDiffs(string npcId, IContextCacheManager cacheManager) { }
        public void UpdateKeyValues(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IBudgetScheduler scheduler) { }
        public void StoreNumericValues(string npcId, Dictionary<string, float> values) { _keyLastNumericValues[npcId] = values; }
        public void ClearNpcDiffs(string npcId) { _diffStore.Remove(npcId); }
        public void RemoveNpcKeyLastValues(string npcId) { _keyLastValues.Remove(npcId); _keyLastNumericValues.Remove(npcId); }
        public bool TryGetDiffStore(string npcId, out List<ContextDiff> diffs) { return _diffStore.TryGetValue(npcId, out diffs!); }
        public bool TryGetKeyLastValues(string npcId, out Dictionary<string, string> values) { return _keyLastValues.TryGetValue(npcId, out values!); }
        public void SetKeyLastValue(string npcId, string key, string value) { if (!_keyLastValues.ContainsKey(npcId)) _keyLastValues[npcId] = new Dictionary<string, string>(); _keyLastValues[npcId][key] = value; }
    }

    internal sealed class DefaultContextLayerBuilder : IContextLayerBuilder
    {
        public ChatMessage? BuildL0(string npcId, string scenario, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager) => null;
        public ChatMessage? BuildL1(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IContextDiffTracker diffTracker) => null;
        public ChatMessage? BuildContextLayer(List<KeyMeta> keys, object? pawn) => null;
        public ChatMessage? BuildL5(List<KeyMeta> keys, object? pawn) => null;
        public ChatMessage? BuildDiffMessage(string npcId, ContextLayer layer, ContextSnapshot snapshot, IContextDiffTracker diffTracker) => null;
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
        public void SetConfig(BudgetSchedulerConfig? config) { }
        public BudgetSchedulerConfig GetConfig() => new BudgetSchedulerConfig();
    }
}
