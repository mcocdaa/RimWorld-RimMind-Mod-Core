using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;

namespace RimMind.Application.Features.Context
{
    /// <summary>
    /// Staleness cache for async context providers. Caches provider results keyed by
    /// (provider key, npcId, pawnId) and respects staleness ticks and invalidation triggers.
    /// </summary>
    public sealed class ProviderCache
    {
        private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = new();
        private readonly IAgentBus? _bus;
        private readonly ILogSink? _log;
        private readonly ITickProvider? _tickProvider;

        private readonly record struct CacheKey(string Key, string NpcId, int PawnId);

        private readonly record struct CacheEntry(string? Value, int ComputedAtTicks);

        public ProviderCache(IAgentBus? bus = null, ILogSink? log = null, ITickProvider? tickProvider = null)
        {
            _bus = bus;
            _log = log;
            _tickProvider = tickProvider;
        }

        /// <summary>
        /// Subscribe to invalidation triggers for a provider definition.
        /// Call this after registering a provider with InvalidationTriggers.
        /// </summary>
        public void SubscribeInvalidation(ContextProviderDef def)
        {
            if (_bus == null || def.InvalidationTriggers == null) return;
            foreach (var eventName in def.InvalidationTriggers)
            {
                _bus.SubscribeByName(eventName, _ => InvalidateKey(def.Key));
            }
        }

        /// <summary>
        /// Get a cached value or compute it via the async provider.
        /// Respects staleness ticks: if the cached value is within the staleness window,
        /// it is returned directly without invoking the provider.
        /// </summary>
        public async Task<string?> GetOrComputeAsync(
            ContextProviderDef def,
            ProviderContext ctx,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var cacheKey = new CacheKey(def.Key, ctx.NpcId ?? "", ctx.PawnId);
            var currentTicks = _tickProvider?.TicksGame ?? 0;

            // StalenessTicks == 0 means "no caching" — always call the provider.
            // StalenessTicks > 0 means "cache for this many ticks" — return cached if within window.
            if (def.StalenessTicks > 0 && _entries.TryGetValue(cacheKey, out var entry))
            {
                if (currentTicks - entry.ComputedAtTicks < def.StalenessTicks)
                {
                    return entry.Value;
                }
            }

            try
            {
                var value = await def.Provider(ctx, ct).ConfigureAwait(false);

                // Only cache if StalenessTicks > 0 (caching enabled)
                if (def.StalenessTicks > 0)
                {
                    _entries[cacheKey] = new CacheEntry(value, currentTicks);
                }

                return value;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _log?.Warning($"[ProviderCache] Provider '{def.Key}' failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Invalidate all cache entries for a given key (across all NPCs and pawns).
        /// </summary>
        public void InvalidateKey(string key)
        {
            var keysToRemove = new List<CacheKey>();
            foreach (var kvp in _entries)
            {
                if (kvp.Key.Key == key)
                    keysToRemove.Add(kvp.Key);
            }
            foreach (var k in keysToRemove)
                _entries.TryRemove(k, out _);
        }

        /// <summary>
        /// Invalidate all cache entries for a given NPC.
        /// </summary>
        public void InvalidateNpc(string npcId)
        {
            var keysToRemove = new List<CacheKey>();
            foreach (var kvp in _entries)
            {
                if (kvp.Key.NpcId == npcId)
                    keysToRemove.Add(kvp.Key);
            }
            foreach (var k in keysToRemove)
                _entries.TryRemove(k, out _);
        }

        public void Clear() => _entries.Clear();

        public int Count => _entries.Count;
    }
}
