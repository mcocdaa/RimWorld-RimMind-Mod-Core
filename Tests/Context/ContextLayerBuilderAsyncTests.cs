using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    public class ContextLayerBuilderAsyncTests
    {
        private static ContextLayerBuilder CreateBuilder()
        {
            var keyProvider = new DefaultContextKeyProvider();
            return new ContextLayerBuilder(keyProvider);
        }

        [Fact]
        public async Task BuildLayerAsync_EmptyKeys_ReturnsEmptyList()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");

            var result = await builder.BuildLayerAsync(new List<KeyMeta>(), null, ctx, null, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task BuildLayerAsync_NullKeys_ReturnsEmptyList()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");

            var result = await builder.BuildLayerAsync(null!, null, ctx, null, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task BuildLayerAsync_SyncProvider_ReturnsEntries()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");
            var key = new KeyMeta("test_key", ContextLayer.L2_Environment, 1f,
                _ => new List<ContextEntry> { new ContextEntry("hello world") }, "TestMod");

            var result = await builder.BuildLayerAsync(new List<KeyMeta> { key }, null, ctx, null, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("hello world", result[0].Content);
        }

        [Fact]
        public async Task BuildLayerAsync_AsyncProvider_ReturnsEntry()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");
            var def = new ContextProviderDef("async_key", ContextLayer.L2_Environment, 1f,
                (c, ct) => Task.FromResult<string?>("async value"));
            var key = new KeyMeta("async_key", ContextLayer.L2_Environment, 1f,
                _ => new List<ContextEntry>(), "TestMod");
            key.Def = def;

            var result = await builder.BuildLayerAsync(new List<KeyMeta> { key }, null, ctx, null, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("async_key", result[0].SourceKey);
            Assert.Equal("async value", result[0].Content);
        }

        [Fact]
        public async Task BuildLayerAsync_AsyncProviderReturnsNull_NoEntryAdded()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");
            var def = new ContextProviderDef("null_key", ContextLayer.L2_Environment, 1f,
                (c, ct) => Task.FromResult<string?>(null));
            var key = new KeyMeta("null_key", ContextLayer.L2_Environment, 1f,
                _ => new List<ContextEntry>(), "TestMod");
            key.Def = def;

            var result = await builder.BuildLayerAsync(new List<KeyMeta> { key }, null, ctx, null, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task BuildLayerAsync_MixedSyncAndAsync_ReturnsAllEntries()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");

            var asyncDef = new ContextProviderDef("async_key", ContextLayer.L2_Environment, 1f,
                (c, ct) => Task.FromResult<string?>("async content"));
            var asyncKey = new KeyMeta("async_key", ContextLayer.L2_Environment, 1f,
                _ => new List<ContextEntry>(), "TestMod");
            asyncKey.Def = asyncDef;

            var syncKey = new KeyMeta("sync_key", ContextLayer.L2_Environment, 1f,
                _ => new List<ContextEntry> { new ContextEntry("sync content") }, "TestMod");

            var result = await builder.BuildLayerAsync(
                new List<KeyMeta> { asyncKey, syncKey }, null, ctx, null, CancellationToken.None);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task BuildLayerAsync_CancellationRequested_ThrowsOperationCanceled()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var key = new KeyMeta("test", ContextLayer.L2_Environment, 1f,
                _ => new List<ContextEntry> { new ContextEntry("content") }, "TestMod");

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                builder.BuildLayerAsync(new List<KeyMeta> { key }, null, ctx, null, cts.Token));
        }

        [Fact]
        public async Task BuildLayerAsync_WithProviderCache_UsesCache()
        {
            var builder = CreateBuilder();
            var ctx = new ProviderContext("dialogue", "trace-1");
            var cache = new ProviderCache();
            var def = new ContextProviderDef("cached_key", ContextLayer.L2_Environment, 1f,
                (c, ct) => Task.FromResult<string?>("cached value"), stalenessTicks: 6000);
            var key = new KeyMeta("cached_key", ContextLayer.L2_Environment, 1f,
                _ => new List<ContextEntry>(), "TestMod");
            key.Def = def;

            // First call should invoke the provider
            var result1 = await builder.BuildLayerAsync(new List<KeyMeta> { key }, null, ctx, cache, CancellationToken.None);
            Assert.Single(result1);
            Assert.Equal("cached value", result1[0].Content);

            // Second call should use cache (same result)
            var result2 = await builder.BuildLayerAsync(new List<KeyMeta> { key }, null, ctx, cache, CancellationToken.None);
            Assert.Single(result2);
            Assert.Equal("cached value", result2[0].Content);
        }

        [Fact]
        public void EntriesToLayerMessage_NullEntries_ReturnsNull()
        {
            var builder = CreateBuilder();
            var result = builder.EntriesToLayerMessage(null!, "L0");
            Assert.Null(result);
        }

        [Fact]
        public void EntriesToLayerMessage_EmptyEntries_ReturnsNull()
        {
            var builder = CreateBuilder();
            var result = builder.EntriesToLayerMessage(new List<ContextEntry>(), "L0");
            Assert.Null(result);
        }

        [Fact]
        public void EntriesToLayerMessage_EntriesWithContent_ReturnsMessage()
        {
            var builder = CreateBuilder();
            var entries = new List<ContextEntry>
            {
                new ContextEntry { SourceKey = "key1", Content = "value1" },
                new ContextEntry { SourceKey = "key2", Content = "value2" }
            };

            var result = builder.EntriesToLayerMessage(entries, "L2");

            Assert.NotNull(result);
            Assert.Equal("system", result.Role);
            Assert.Equal("L2", result.LayerTag);
            Assert.Contains("[key1] value1", result.Content);
            Assert.Contains("[key2] value2", result.Content);
        }

        [Fact]
        public void EntriesToLayerMessage_EntriesWithEmptyContent_SkipsEmpty()
        {
            var builder = CreateBuilder();
            var entries = new List<ContextEntry>
            {
                new ContextEntry { SourceKey = "key1", Content = "value1" },
                new ContextEntry { SourceKey = "key2", Content = "" },
                new ContextEntry { SourceKey = "key3", Content = null! }
            };

            var result = builder.EntriesToLayerMessage(entries, "L0");

            Assert.NotNull(result);
            Assert.Contains("[key1] value1", result.Content);
            Assert.DoesNotContain("[key2]", result.Content);
            Assert.DoesNotContain("[key3]", result.Content);
        }

        [Fact]
        public void EntriesToLayerMessage_AllEmptyContent_ReturnsNull()
        {
            var builder = CreateBuilder();
            var entries = new List<ContextEntry>
            {
                new ContextEntry { SourceKey = "key1", Content = "" },
                new ContextEntry { SourceKey = "key2", Content = null! }
            };

            var result = builder.EntriesToLayerMessage(entries, "L3");
            Assert.Null(result);
        }
    }
}
