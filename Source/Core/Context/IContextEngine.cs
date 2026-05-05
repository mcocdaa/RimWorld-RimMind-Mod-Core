namespace RimMind.Core.Context
{
    public interface IContextEngine
    {
        ContextSnapshot BuildSnapshot(ContextRequest request);
        int GetL0CacheCount();
        int GetL1BlockCacheCount();
        int GetDiffStoreCount();
        int GetEmbedCacheCount();
        void ResetCaches();
    }
}
