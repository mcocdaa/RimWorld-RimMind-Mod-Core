using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Contracts.Client;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Context;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Pipeline;
using Xunit;

namespace RimMind.Tests.Pipeline.Context
{
    internal sealed class ContextBuildTestMiddleware : IMiddleware<ContextBuildContext>
    {
        private readonly string _name;
        private readonly int _order;
        private readonly bool _shortCircuit;

        public ContextBuildTestMiddleware(string name, int order = 0, bool shortCircuit = false)
        {
            _name = name;
            _order = order;
            _shortCircuit = shortCircuit;
        }

        public string Id => _name;
        public string Name => _name;
        public int Order => _order;

        public async Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            ((List<string>)context.Items["log"]!).Add(_name);

            if (_shortCircuit)
            {
                context.ShortCircuit($"short_circuited_by_{_name}");
                return;
            }

            await next(context);
        }
    }

    public class ContextBuildPipelineTests
    {
        private static ContextBuildContext CreateContext()
        {
            return new ContextBuildContext
            {
                Request = new ContextRequest
                {
                    NpcId = "NPC-1",
                    Scenario = "dialogue",
                },
            };
        }

        [Fact]
        public async Task Pipeline_ExecutesInOrder()
        {
            var middlewares = new IMiddleware<ContextBuildContext>[]
            {
                new ContextBuildTestMiddleware("A", order: 0),
                new ContextBuildTestMiddleware("B", order: 1),
                new ContextBuildTestMiddleware("C", order: 2),
            };

            var pipeline = new Pipeline<ContextBuildContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            var log = (List<string>)context.Items["log"]!;
            Assert.Equal(new[] { "A", "B", "C" }, log);
        }

        [Fact]
        public async Task ShortCircuit_StopsSubsequentMiddlewares()
        {
            var middlewares = new IMiddleware<ContextBuildContext>[]
            {
                new ContextBuildTestMiddleware("A", order: 0, shortCircuit: true),
                new ContextBuildTestMiddleware("B", order: 1),
                new ContextBuildTestMiddleware("C", order: 2),
            };

            var pipeline = new Pipeline<ContextBuildContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            var log = (List<string>)context.Items["log"]!;
            Assert.Equal(new[] { "A" }, log);
            Assert.True(context.IsShortCircuited);
        }

        [Fact]
        public async Task Pipeline_ExecutesMiddlewaresInOrder_WithFourMiddlewares()
        {
            var middlewares = new IMiddleware<ContextBuildContext>[]
            {
                new ContextBuildTestMiddleware("W", order: 0),
                new ContextBuildTestMiddleware("X", order: 1),
                new ContextBuildTestMiddleware("Y", order: 2),
                new ContextBuildTestMiddleware("Z", order: 3),
            };

            var pipeline = new Pipeline<ContextBuildContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            var log = (List<string>)context.Items["log"]!;
            Assert.Equal(new[] { "W", "X", "Y", "Z" }, log);
        }

        [Fact]
        public async Task CacheLookup_CacheHit_ShortCircuits()
        {
            var cacheManager = new StubContextCacheManager(hasL0Cache: true);
            var lookup = new CacheLookupMiddleware(cacheManager);
            var pipeline = new Pipeline<ContextBuildContext>(
                new IMiddleware<ContextBuildContext>[] { lookup });

            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("cache_hit", context.ShortCircuitReason);
            Assert.NotNull(context.Snapshot);
        }

        [Fact]
        public async Task CacheLookup_CacheMiss_ProceedsToNext()
        {
            var cacheManager = new StubContextCacheManager(hasL0Cache: false);
            var lookup = new CacheLookupMiddleware(cacheManager);
            var next = new ContextBuildTestMiddleware("Next", order: 0);
            var pipeline = new Pipeline<ContextBuildContext>(
                new IMiddleware<ContextBuildContext>[] { lookup, next });

            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            Assert.False(context.IsShortCircuited);
            var log = (List<string>)context.Items["log"]!;
            Assert.Contains("Next", log);
        }

        private sealed class StubContextCacheManager : IContextCacheManager
        {
            private readonly bool _hasL0Cache;
            private readonly ChatMessage? _cachedMsg;

            public StubContextCacheManager(bool hasL0Cache)
            {
                _hasL0Cache = hasL0Cache;
                if (hasL0Cache)
                {
                    _cachedMsg = new ChatMessage { Role = "system", Content = "cached" };
                }
            }

            public IReadOnlyDictionary<string, ChatMessage> L0Cache { get; }
                = new Dictionary<string, ChatMessage>();
            public IReadOnlyDictionary<string, Dictionary<string, string>> L1BlockCache { get; }
                = new Dictionary<string, Dictionary<string, string>>();
            public IReadOnlyDictionary<string, int> L1Version { get; }
                = new Dictionary<string, int>();
            public IReadOnlyDictionary<string, Dictionary<string, int>> L1KeyVersions { get; }
                = new Dictionary<string, Dictionary<string, int>>();
            public IReadOnlyDictionary<string, bool> PendingCacheEvents { get; }
                = new Dictionary<string, bool>();
            public EmbedCache EmbedCache { get; }
                = new EmbedCache();

            public void TouchCache(string cacheKey) { }
            public void RemoveL0CacheForNpc(string npcId) { }
            public void InvalidateLayer(string npcId, ContextLayer layer) { }
            public void InvalidateKey(string npcId, string key) { }
            public void UpdateBaseline(string npcId) { }
            public void InvalidateNpc(string npcId) { }
            public void Reset() { }
            public int GetL0CacheCount() => 0;
            public int GetL1BlockCacheCount() => 0;
            public int GetEmbedCacheCount() => 0;
            public void ClearPendingCacheEvents() { }

            public bool TryGetL0CacheItem(string key, out ChatMessage msg)
            {
                if (_hasL0Cache && _cachedMsg != null)
                {
                    msg = _cachedMsg;
                    return true;
                }
                msg = null!;
                return false;
            }

            public void SetL0CacheItem(string key, ChatMessage msg) { }
            public bool RemoveL0CacheItem(string key) => false;
            public bool TryGetL1BlockCache(string npcId, out Dictionary<string, string> blocks)
            { blocks = null!; return false; }
            public void SetL1BlockCache(string npcId, Dictionary<string, string> blocks) { }
            public bool TryGetL1Version(string npcId, out int version)
            { version = 0; return false; }
            public void SetL1Version(string npcId, int version) { }
            public bool TryGetL1KeyVersions(string npcId, out Dictionary<string, int> versions)
            { versions = null!; return false; }
            public void SetL1KeyVersions(string npcId, Dictionary<string, int> versions) { }
            public bool TryGetPendingCacheEvent(string key, out bool value)
            { value = false; return false; }
            public void SetPendingCacheEvent(string key, bool value) { }
        }
    }
}
