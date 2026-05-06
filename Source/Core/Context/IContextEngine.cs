using System;
using RimMind.Contracts;

namespace RimMind.Core.Context
{
    public interface IContextEngine : IDisposable
    {
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        ContextSnapshot? BuildSnapshot(ContextRequest request);
        int GetL0CacheCount();
        int GetL1BlockCacheCount();
        int GetDiffStoreCount();
        int GetEmbedCacheCount();
        void ResetCaches();
        void TouchCache(string cacheKey);
        void RemoveL0CacheForNpc(string npcId);
        void InvalidateLayer(string npcId, ContextLayer layer);
        void InvalidateKey(string npcId, string key);
        void UpdateBaseline(string npcId);
        void InvalidateNpc(string npcId);
        IBudgetScheduler? GetScheduler();
        EmbeddingSnapshotStore? GetEmbeddingSnapshotStore();
    }
}
