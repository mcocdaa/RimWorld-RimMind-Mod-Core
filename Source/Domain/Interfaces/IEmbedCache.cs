namespace RimMind.Domain.Interfaces
{
    public interface IEmbedCache
    {
        float[]? GetOrComputeQueryEmbedding(string query);
        void StoreEntryEmbedding(string key, float[] embedding);
        float[]? GetEntryEmbedding(string key);
    }
}
