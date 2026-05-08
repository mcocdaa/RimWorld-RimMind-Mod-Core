using System;
using System.Collections.Generic;
using System.Threading;
using RimMind.Contracts.Client;
using RimMind.Core.Internal;
using RimMind.Contracts.Npc;
using RimMind.Kernel.Context;

namespace RimMind.Kernel.Context
{
    public class ContextEngine : IContextEngine, IDisposable
    {
        private bool _disposed;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        private readonly IBudgetScheduler _scheduler;
        private readonly IHistoryManager _historyManager;
        private readonly IContextCacheManager _cacheManager;
        private readonly IContextDiffTracker _diffTracker;
        private readonly INpcManager _npcManager;
        private readonly EmbeddingSnapshotStore _embeddingSnapshotStore = new EmbeddingSnapshotStore();

        private readonly ContextOrchestrator _orchestrator;
        private readonly ContextTelemetryEmitter _telemetryEmitter;

        public ContextEngine(IHistoryManager historyManager,
            INpcManager? npcManager = null,
            IContextCacheManager? cacheManager = null,
            IContextDiffTracker? diffTracker = null,
            IContextLayerBuilder? layerBuilder = null,
            IBudgetScheduler? scheduler = null)
        {
            _historyManager = historyManager;
            _npcManager = npcManager ?? RimMindServiceLocator.Get<INpcManager>() ?? new NullNpcManager();
            _cacheManager = cacheManager ?? new ContextCacheManager();
            _diffTracker = diffTracker ?? new ContextDiffTracker();
            var resolvedLayerBuilder = layerBuilder ?? new ContextLayerBuilder();
            _scheduler = scheduler ?? new BudgetScheduler();

            _orchestrator = new ContextOrchestrator(
                _historyManager, _npcManager, _cacheManager, _diffTracker,
                resolvedLayerBuilder, _scheduler);

            _telemetryEmitter = new ContextTelemetryEmitter(_embeddingSnapshotStore);
        }

        public IBudgetScheduler GetScheduler() => _scheduler;
        public EmbeddingSnapshotStore GetEmbeddingSnapshotStore() => _embeddingSnapshotStore;

        public void TouchCache(string cacheKey)
        {
            if (_disposed) return;
            _cacheManager.TouchCache(cacheKey);
        }

        public void RemoveL0CacheForNpc(string npcId)
        {
            if (_disposed) return;
            _cacheManager.RemoveL0CacheForNpc(npcId);
        }

        public ContextSnapshot? BuildSnapshot(ContextRequest request)
        {
            if (_disposed) return null;
            _rwLock.EnterWriteLock();
            try
            {
                var snapshot = _orchestrator.BuildSnapshotCore(request);
                if (snapshot != null)
                    CommitSnapshotSideEffects(snapshot);
                return snapshot;
            }
            finally
            {
                ContextKeyRegistry.CurrentScenario = string.Empty;
                _cacheManager.ClearPendingCacheEvents();
                _rwLock.ExitWriteLock();
            }
        }

        private void CommitSnapshotSideEffects(ContextSnapshot snapshot)
        {
            var payload = snapshot._commitPayload;
            if (payload == null) return;

            var pawn = payload.Pawn;
            var filteredKeys = payload.FilteredKeys;
            var schedule = payload.Schedule;

            _diffTracker.UpdateKeyValues(snapshot.NpcId, filteredKeys!, pawn, _cacheManager, _scheduler);

            _diffTracker.MergeExpiredDiffs(snapshot.NpcId, _cacheManager);

            if (_cacheManager.PendingCacheEvents.Count > 0)
            {
                foreach (var kvp in _cacheManager.PendingCacheEvents)
                    snapshot.SetCacheHitEvent(kvp.Key, kvp.Value);
            }

            if (filteredKeys != null)
            {
                foreach (var key in filteredKeys)
                {
                    if (key.UpdateCount > 0)
                        snapshot.KeyChangeCounts[key.Key] = key.UpdateCount;
                }
            }

            _telemetryEmitter.EmitForSnapshot(snapshot, filteredKeys!, schedule!, pawn, _diffTracker, _cacheManager);

            snapshot._commitPayload = null;
        }

        public void InvalidateLayer(string npcId, ContextLayer layer)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.InvalidateLayer(npcId, layer);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void InvalidateKey(string npcId, string key)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.InvalidateKey(npcId, key);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void UpdateBaseline(string npcId)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.UpdateBaseline(npcId);
                if (_diffTracker.TryGetDiffStore(npcId, out var diffs))
                    diffs.Clear();
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void InvalidateNpc(string npcId)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.InvalidateNpc(npcId);
                _diffTracker.ClearNpcDiffs(npcId);
                _diffTracker.RemoveNpcKeyLastValues(npcId);
                _historyManager.ClearHistory(npcId);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _rwLock?.Dispose();
        }

        public void ResetCaches()
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.Reset();
                _diffTracker.Reset();
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public int GetL0CacheCount()
        {
            _rwLock.EnterReadLock();
            try { return _cacheManager.GetL0CacheCount(); }
            finally { _rwLock.ExitReadLock(); }
        }

        public int GetL1BlockCacheCount()
        {
            _rwLock.EnterReadLock();
            try { return _cacheManager.GetL1BlockCacheCount(); }
            finally { _rwLock.ExitReadLock(); }
        }

        public int GetDiffStoreCount()
        {
            _rwLock.EnterReadLock();
            try { return _diffTracker.GetDiffStoreCount(); }
            finally { _rwLock.ExitReadLock(); }
        }

        public int GetEmbedCacheCount()
        {
            return _cacheManager.GetEmbedCacheCount();
        }
    }

    internal class NullNpcManager : INpcManager
    {
        public Verse.Pawn? FindPawnByNpcId(string npcId) => null;
        public Verse.Pawn? FindProxyPawnForMap(Verse.Map map) => null;
        public void SpawnNpc(NpcProfile profile) { }
        public void KillNpc(string npcId) { }
        public bool IsNpcAlive(string npcId) => false;
        public NpcProfile? GetNpc(string npcId) => null;
        public IReadOnlyList<NpcProfile> GetAllNpcs() => new List<NpcProfile>();
        public string GetNpcForMap(Verse.Map map) => "";
        public void RegisterActiveAgent(int thingId) { }
        public void UnregisterActiveAgent(int thingId) { }
        public HashSet<int> GetActiveAgentPawnIds() => new HashSet<int>();
        public void IndexPawn(Verse.Pawn pawn) { }
        public void UnindexPawn(int thingId) { }
        public string GetMapNpcId(Verse.Map map) => "";
    }
}
