using Xunit;

namespace RimMind.Tests.Domain.ValueObjects
{
    public class EmbedCacheTests
    {
        private static float[] MakeEmbedding(int size = 4)
        {
            var emb = new float[size];
            for (int i = 0; i < size; i++) emb[i] = i * 0.1f;
            return emb;
        }

        [Fact]
        public void SetBlockEmbedding_GetBlockEmbedding_RoundTrip()
        {
            var cache = new EmbedCache();
            var emb = MakeEmbedding();

            cache.SetBlockEmbedding("npc1", "key1", emb);
            var result = cache.GetBlockEmbedding("npc1", "key1");

            Assert.NotNull(result);
            Assert.Equal(emb, result);
        }

        [Fact]
        public void SetEntryEmbedding_GetEntryEmbedding_RoundTrip()
        {
            var cache = new EmbedCache();
            var emb = MakeEmbedding();

            cache.SetEntryEmbedding("npc1", "key1", 0, emb);
            var result = cache.GetEntryEmbedding("npc1", "key1", 0);

            Assert.NotNull(result);
            Assert.Equal(emb, result);
        }

        [Fact]
        public void GetBlockEmbedding_NotFound_ReturnsNull()
        {
            var cache = new EmbedCache();

            var result = cache.GetBlockEmbedding("npc1", "key1");

            Assert.Null(result);
        }

        [Fact]
        public void GetEntryEmbedding_NotFound_ReturnsNull()
        {
            var cache = new EmbedCache();

            var result = cache.GetEntryEmbedding("npc1", "key1", 0);

            Assert.Null(result);
        }

        [Fact]
        public void InvalidateBlock_DecrementsCountAndRemoves()
        {
            var cache = new EmbedCache();
            cache.SetBlockEmbedding("npc1", "key1", MakeEmbedding());
            cache.SetBlockEmbedding("npc1", "key2", MakeEmbedding());

            Assert.Equal(2, cache.Count);

            cache.InvalidateBlock("npc1", "key1");

            Assert.Equal(1, cache.Count);
            Assert.Null(cache.GetBlockEmbedding("npc1", "key1"));
            Assert.NotNull(cache.GetBlockEmbedding("npc1", "key2"));
        }

        [Fact]
        public void InvalidateEntries_DecrementsCountAndRemoves()
        {
            var cache = new EmbedCache();
            cache.SetEntryEmbedding("npc1", "key1", 0, MakeEmbedding());
            cache.SetEntryEmbedding("npc1", "key1", 1, MakeEmbedding());
            cache.SetEntryEmbedding("npc1", "key2", 0, MakeEmbedding());

            Assert.Equal(3, cache.Count);

            cache.InvalidateEntries("npc1", "key1");

            Assert.Equal(1, cache.Count);
            Assert.Null(cache.GetEntryEmbedding("npc1", "key1", 0));
            Assert.Null(cache.GetEntryEmbedding("npc1", "key1", 1));
            Assert.NotNull(cache.GetEntryEmbedding("npc1", "key2", 0));
        }

        [Fact]
        public void InvalidateNpc_ClearsAllBlockAndEntryForNpc()
        {
            var cache = new EmbedCache();
            cache.SetBlockEmbedding("npc1", "key1", MakeEmbedding());
            cache.SetBlockEmbedding("npc1", "key2", MakeEmbedding());
            cache.SetEntryEmbedding("npc1", "ekey1", 0, MakeEmbedding());
            cache.SetBlockEmbedding("npc2", "key1", MakeEmbedding());
            cache.SetEntryEmbedding("npc2", "ekey1", 0, MakeEmbedding());

            cache.InvalidateNpc("npc1");

            Assert.Equal(2, cache.Count);
            Assert.Null(cache.GetBlockEmbedding("npc1", "key1"));
            Assert.Null(cache.GetBlockEmbedding("npc1", "key2"));
            Assert.Null(cache.GetEntryEmbedding("npc1", "ekey1", 0));
            Assert.NotNull(cache.GetBlockEmbedding("npc2", "key1"));
            Assert.NotNull(cache.GetEntryEmbedding("npc2", "ekey1", 0));
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            var cache = new EmbedCache();
            cache.SetBlockEmbedding("npc1", "key1", MakeEmbedding());
            cache.SetEntryEmbedding("npc1", "ekey1", 0, MakeEmbedding());

            cache.Clear();

            Assert.Equal(0, cache.Count);
            Assert.Null(cache.GetBlockEmbedding("npc1", "key1"));
            Assert.Null(cache.GetEntryEmbedding("npc1", "ekey1", 0));
        }

        [Fact]
        public void Count_ReflectsTotalEntries()
        {
            var cache = new EmbedCache();

            Assert.Equal(0, cache.Count);

            cache.SetBlockEmbedding("npc1", "key1", MakeEmbedding());
            Assert.Equal(1, cache.Count);

            cache.SetEntryEmbedding("npc1", "ekey1", 0, MakeEmbedding());
            Assert.Equal(2, cache.Count);

            cache.SetEntryEmbedding("npc1", "ekey1", 1, MakeEmbedding());
            Assert.Equal(3, cache.Count);
        }
    }
}
