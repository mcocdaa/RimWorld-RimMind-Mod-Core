using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Registry;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    public sealed class ContextKeyRegistryImpl : IContextKeyRegistry, IOwnedRegistry
    {
        private readonly ConcurrentDictionary<string, KeyMeta> _keys = new ConcurrentDictionary<string, KeyMeta>();
        private readonly ILogSink? _logSink;

        public ContextKeyRegistryImpl(ILogSink? logSink = null)
        {
            _logSink = logSink;
        }

        public void Register(KeyMeta meta)
        {
            if (_keys.ContainsKey(meta.Key))
            {
                var old = _keys[meta.Key];
                _logSink?.Warning($"[RimMind-Core] ContextKey '{meta.Key}' registered by '{old.OwnerMod}' " +
                    $"overwritten by '{meta.OwnerMod}'.");
                meta.OverrideSource = old.OwnerMod ?? "Unknown";
            }
            _keys[meta.Key] = meta;
        }

        public void Register(ContextProviderDef def)
        {
            var meta = new KeyMeta(def.Key, def.Layer, def.Priority, _ => new List<ContextEntry>(), def.OwnerMod ?? "Unknown",
                cacheScope: def.CacheScope)
            {
                Def = def
            };
            Register(meta);
        }

        public bool Unregister(string key)
        {
            return _keys.TryRemove(key, out _);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// KeyMeta 使用 <c>OwnerMod</c> 字段（非 <c>OwnerModId</c>），语义一致。
        /// </remarks>
        public int UnregisterByOwner(string ownerModId)
        {
            if (ownerModId == null) throw new ArgumentNullException(nameof(ownerModId));
            var toRemove = _keys.Values
                .Where(k => k.OwnerMod == ownerModId)
                .Select(k => k.Key)
                .ToList();
            foreach (var key in toRemove)
            {
                _keys.TryRemove(key, out _);
            }
            return toRemove.Count;
        }

        public IReadOnlyList<KeyMeta> GetAll()
        {
            return new List<KeyMeta>(_keys.Values);
        }

        public KeyMeta? Get(string key)
        {
            return _keys.TryGetValue(key, out var meta) ? meta : null;
        }

        public void Clear()
        {
            _keys.Clear();
        }
    }
}
