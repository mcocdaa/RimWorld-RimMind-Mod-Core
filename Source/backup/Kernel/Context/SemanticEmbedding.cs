using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RimMind.Kernel.Context
{
    public static class SemanticEmbedding
    {
        private static readonly ConcurrentDictionary<(string, string), float[]> _blockEmbeddings
            = new ConcurrentDictionary<(string, string), float[]>();
        private static readonly ConcurrentDictionary<(string, string, int), float[]> _entryEmbeddings
            = new ConcurrentDictionary<(string, string, int), float[]>();

        public static void SetBlockEmbedding(string npcId, string key, float[] embedding)
        {
            _blockEmbeddings[(npcId, key)] = embedding;
        }

        public static float[]? GetBlockEmbedding(string npcId, string key)
        {
            return _blockEmbeddings.TryGetValue((npcId, key), out var emb) ? emb : null;
        }

        public static void SetEntryEmbedding(string npcId, string key, int entryIndex, float[] embedding)
        {
            _entryEmbeddings[(npcId, key, entryIndex)] = embedding;
        }

        public static float[]? GetEntryEmbedding(string npcId, string key, int entryIndex)
        {
            return _entryEmbeddings.TryGetValue((npcId, key, entryIndex), out var emb) ? emb : null;
        }

        public static void InvalidateBlockEmbedding(string npcId, string key)
        {
            _blockEmbeddings.TryRemove((npcId, key), out _);
        }

        public static void InvalidateEntryEmbeddings(string npcId, string key)
        {
            var keysToRemove = new List<(string, string, int)>();
            foreach (var kvp in _entryEmbeddings)
            {
                if (kvp.Key.Item1 == npcId && kvp.Key.Item2 == key)
                    keysToRemove.Add(kvp.Key);
            }
            foreach (var k in keysToRemove)
                _entryEmbeddings.TryRemove(k, out _);
        }

        public static void InvalidateNpc(string npcId)
        {
            var blockKeysToRemove = new List<(string, string)>();
            foreach (var kvp in _blockEmbeddings)
            {
                if (kvp.Key.Item1 == npcId)
                    blockKeysToRemove.Add(kvp.Key);
            }
            foreach (var k in blockKeysToRemove)
                _blockEmbeddings.TryRemove(k, out _);

            var entryKeysToRemove = new List<(string, string, int)>();
            foreach (var kvp in _entryEmbeddings)
            {
                if (kvp.Key.Item1 == npcId)
                    entryKeysToRemove.Add(kvp.Key);
            }
            foreach (var k in entryKeysToRemove)
                _entryEmbeddings.TryRemove(k, out _);
        }

        public static void Clear()
        {
            _blockEmbeddings.Clear();
            _entryEmbeddings.Clear();
        }
    }
}
