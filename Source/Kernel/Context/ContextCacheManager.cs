using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Core;
using RimMind.Core.Client;
using RimMind.Core.Internal;
using RimMind.Core.Settings;

namespace RimMind.Kernel.Context
{
    internal class ContextCacheManager : IContextCacheManager
    {
        private readonly Dictionary<string, ChatMessage> _l0Cache = new Dictionary<string, ChatMessage>();
        private readonly Dictionary<string, Dictionary<string, string>> _l1BlockCache = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, int> _l1Version = new Dictionary<string, int>();
        private readonly Dictionary<string, Dictionary<string, int>> _l1KeyVersions = new Dictionary<string, Dictionary<string, int>>();
        private readonly LinkedList<string> _cacheOrder = new LinkedList<string>();
        private readonly Dictionary<string, LinkedListNode<string>> _cacheOrderIndex = new Dictionary<string, LinkedListNode<string>>();
        private readonly Dictionary<string, bool> _pendingCacheEvents = new Dictionary<string, bool>();
        private readonly EmbedCache _embedCache = new EmbedCache();

        private int MaxCacheEntries => RimMindCoreMod.Settings?.Context?.maxCacheEntries ?? 100;

        public IReadOnlyDictionary<string, ChatMessage> L0Cache => _l0Cache;
        public IReadOnlyDictionary<string, Dictionary<string, string>> L1BlockCache => _l1BlockCache;
        public IReadOnlyDictionary<string, int> L1Version => _l1Version;
        public IReadOnlyDictionary<string, Dictionary<string, int>> L1KeyVersions => _l1KeyVersions;
        public IReadOnlyDictionary<string, bool> PendingCacheEvents => _pendingCacheEvents;
        public EmbedCache EmbedCache => _embedCache;

        public void TouchCache(string cacheKey)
        {
            if (_cacheOrderIndex.TryGetValue(cacheKey, out var node))
            {
                _cacheOrder.Remove(node);
                _cacheOrder.AddLast(node);
            }
            else
            {
                node = _cacheOrder.AddLast(cacheKey);
                _cacheOrderIndex[cacheKey] = node;
            }
            while (_cacheOrder.Count > MaxCacheEntries)
            {
                var oldest = _cacheOrder.First!.Value;
                _cacheOrder.RemoveFirst();
                _cacheOrderIndex.Remove(oldest);
                _l0Cache.Remove(oldest);
                string oldestNpc = oldest.Contains("_") ? oldest.Substring(0, oldest.LastIndexOf('_')) : oldest;
                _l1BlockCache.Remove(oldestNpc);
                _l1Version.Remove(oldestNpc);
                _l1KeyVersions.Remove(oldestNpc);
                _embedCache.InvalidateNpc(oldestNpc);
                SemanticEmbedding.InvalidateNpc(oldestNpc);
            }
        }

        public void RemoveL0CacheForNpc(string npcId)
        {
            var keysToRemove = new List<string>();
            foreach (var key in _l0Cache.Keys)
            {
                if (key == npcId || key.StartsWith(npcId + "_"))
                    keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                _l0Cache.Remove(key);
                if (_cacheOrderIndex.TryGetValue(key, out var node))
                {
                    _cacheOrder.Remove(node);
                    _cacheOrderIndex.Remove(key);
                }
            }
        }

        public void InvalidateLayer(string npcId, ContextLayer layer)
        {
            if (layer == ContextLayer.L0_Static)
                RemoveL0CacheForNpc(npcId);
            if (layer == ContextLayer.L1_Baseline)
            {
                _l1BlockCache.Remove(npcId);
                _l1Version.Remove(npcId);
                _l1KeyVersions.Remove(npcId);
            }
        }

        public void InvalidateKey(string npcId, string key)
        {
            if (_l1BlockCache.TryGetValue(npcId, out var blocks))
                blocks.Remove(key);
            _embedCache.InvalidateBlock(npcId, key);
            _embedCache.InvalidateEntries(npcId, key);
            SemanticEmbedding.InvalidateBlockEmbedding(npcId, key);
            SemanticEmbedding.InvalidateEntryEmbeddings(npcId, key);
        }

        public void UpdateBaseline(string npcId)
        {
            _l1BlockCache.Remove(npcId);
            _l1Version.Remove(npcId);
            _l1KeyVersions.Remove(npcId);
        }

        public void InvalidateNpc(string npcId)
        {
            RemoveL0CacheForNpc(npcId);
            _l1BlockCache.Remove(npcId);
            _l1Version.Remove(npcId);
            _l1KeyVersions.Remove(npcId);
            _embedCache.InvalidateNpc(npcId);
            SemanticEmbedding.InvalidateNpc(npcId);
        }

        public void Reset()
        {
            _l0Cache.Clear();
            _l1BlockCache.Clear();
            _l1Version.Clear();
            _l1KeyVersions.Clear();
            _cacheOrder.Clear();
            _cacheOrderIndex.Clear();
            _pendingCacheEvents.Clear();
            _embedCache.Clear();
        }

        public int GetL0CacheCount() => _l0Cache.Count;
        public int GetL1BlockCacheCount() => _l1BlockCache.Count;
        public int GetEmbedCacheCount() => _embedCache.Count;

        public void ClearPendingCacheEvents()
        {
            _pendingCacheEvents.Clear();
        }

        public bool TryGetL0CacheItem(string key, out ChatMessage msg)
        {
            return _l0Cache.TryGetValue(key, out msg!);
        }

        public void SetL0CacheItem(string key, ChatMessage msg)
        {
            _l0Cache[key] = msg;
        }

        public bool RemoveL0CacheItem(string key)
        {
            return _l0Cache.Remove(key);
        }

        public bool TryGetL1BlockCache(string npcId, out Dictionary<string, string> blocks)
        {
            return _l1BlockCache.TryGetValue(npcId, out blocks!);
        }

        public void SetL1BlockCache(string npcId, Dictionary<string, string> blocks)
        {
            _l1BlockCache[npcId] = blocks;
        }

        public bool TryGetL1Version(string npcId, out int version)
        {
            return _l1Version.TryGetValue(npcId, out version);
        }

        public void SetL1Version(string npcId, int version)
        {
            _l1Version[npcId] = version;
        }

        public bool TryGetL1KeyVersions(string npcId, out Dictionary<string, int> versions)
        {
            return _l1KeyVersions.TryGetValue(npcId, out versions!);
        }

        public void SetL1KeyVersions(string npcId, Dictionary<string, int> versions)
        {
            _l1KeyVersions[npcId] = versions;
        }

        public bool TryGetPendingCacheEvent(string key, out bool value)
        {
            return _pendingCacheEvents.TryGetValue(key, out value);
        }

        public void SetPendingCacheEvent(string key, bool value)
        {
            _pendingCacheEvents[key] = value;
        }
    }
}
