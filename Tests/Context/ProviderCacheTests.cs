using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    /// <summary>
    /// Test-only ITickProvider that allows manual tick control.
    /// </summary>
    internal sealed class ManualTickProvider : ITickProvider
    {
        public int CurrentTick { get; set; }
        public int TicksGame => CurrentTick;
    }

    public class ProviderCacheTests
    {
        private static ContextProviderDef CreateDef(
            string key = "test",
            int stalenessTicks = 0,
            Func<ProviderContext, CancellationToken, Task<string?>>? provider = null,
            IReadOnlyList<string>? invalidationTriggers = null)
        {
            return new ContextProviderDef(
                key: key,
                layer: ContextLayer.L2_Environment,
                priority: 1.0f,
                provider: provider ?? ((_, _) => Task.FromResult<string?>("default_value")),
                stalenessTicks: stalenessTicks,
                invalidationTriggers: invalidationTriggers);
        }

        private static ProviderContext CreateCtx(string npcId = "npc-1", int pawnId = 0)
        {
            return new ProviderContext("dialogue", "trace-1")
            {
                NpcId = npcId,
                PawnId = pawnId
            };
        }

        [Fact]
        public async Task GetOrCompute_NoStaleness_CallsProviderEveryTime()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            int callCount = 0;
            var def = CreateDef(stalenessTicks: 0, provider: (_, _) =>
            {
                callCount++;
                return Task.FromResult<string?>($"value_{callCount}");
            });
            var cache = new ProviderCache(tickProvider: tickProvider);
            var ctx = CreateCtx();

            var result1 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            var result2 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);

            Assert.Equal(2, callCount);
            Assert.Equal("value_1", result1);
            Assert.Equal("value_2", result2);
        }

        [Fact]
        public async Task GetOrCompute_WithStaleness_ReturnsCachedWithinWindow()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            int callCount = 0;
            var def = CreateDef(stalenessTicks: 600, provider: (_, _) =>
            {
                callCount++;
                return Task.FromResult<string?>($"value_{callCount}");
            });
            var cache = new ProviderCache(tickProvider: tickProvider);
            var ctx = CreateCtx();

            var result1 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Equal(1, callCount);

            // Within staleness window - should return cached
            tickProvider.CurrentTick = 500;
            var result2 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Equal(1, callCount);
            Assert.Equal(result1, result2);
        }

        [Fact]
        public async Task GetOrCompute_WithStaleness_RefreshesAfterExpiry()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            int callCount = 0;
            var def = CreateDef(stalenessTicks: 600, provider: (_, _) =>
            {
                callCount++;
                return Task.FromResult<string?>($"value_{callCount}");
            });
            var cache = new ProviderCache(tickProvider: tickProvider);
            var ctx = CreateCtx();

            var result1 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Equal(1, callCount);

            // After staleness window - should recompute
            tickProvider.CurrentTick = 800;
            var result2 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Equal(2, callCount);
            Assert.Equal("value_2", result2);
        }

        [Fact]
        public async Task GetOrCompute_StalenessExactBoundary_Refreshes()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            int callCount = 0;
            var def = CreateDef(stalenessTicks: 600, provider: (_, _) =>
            {
                callCount++;
                return Task.FromResult<string?>($"value_{callCount}");
            });
            var cache = new ProviderCache(tickProvider: tickProvider);
            var ctx = CreateCtx();

            await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Equal(1, callCount);

            // Exactly at staleness boundary (100 + 600 = 700)
            tickProvider.CurrentTick = 700;
            await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task GetOrCompute_DifferentNpcs_Isolated()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            int callCount = 0;
            var def = CreateDef(stalenessTicks: 600, provider: (ctx, _) =>
            {
                callCount++;
                return Task.FromResult<string?>($"value_for_{ctx.NpcId}_{callCount}");
            });
            var cache = new ProviderCache(tickProvider: tickProvider);

            var ctx1 = CreateCtx(npcId: "npc-A");
            var ctx2 = CreateCtx(npcId: "npc-B");

            var result1 = await cache.GetOrComputeAsync(def, ctx1, CancellationToken.None);
            var result2 = await cache.GetOrComputeAsync(def, ctx2, CancellationToken.None);

            Assert.Equal(2, callCount);
            Assert.Equal("value_for_npc-A_1", result1);
            Assert.Equal("value_for_npc-B_2", result2);
        }

        [Fact]
        public async Task GetOrCompute_DifferentPawnIds_Isolated()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            int callCount = 0;
            var def = CreateDef(stalenessTicks: 600, provider: (ctx, _) =>
            {
                callCount++;
                return Task.FromResult<string?>($"pawn_{ctx.PawnId}_{callCount}");
            });
            var cache = new ProviderCache(tickProvider: tickProvider);

            var ctx1 = CreateCtx(npcId: "npc-1", pawnId: 10);
            var ctx2 = CreateCtx(npcId: "npc-1", pawnId: 20);

            var result1 = await cache.GetOrComputeAsync(def, ctx1, CancellationToken.None);
            var result2 = await cache.GetOrComputeAsync(def, ctx2, CancellationToken.None);

            Assert.Equal(2, callCount);
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public async Task GetOrCompute_ProviderThrows_ReturnsNull()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            var def = CreateDef(provider: (_, _) => throw new InvalidOperationException("boom"));
            var cache = new ProviderCache(tickProvider: tickProvider);
            var ctx = CreateCtx();

            var result = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrCompute_Cancelled_ThrowsOperationCanceledException()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            var def = CreateDef(provider: (_, ct) => Task.FromResult<string?>("value"));
            var cache = new ProviderCache(tickProvider: tickProvider);
            var ctx = CreateCtx();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                cache.GetOrComputeAsync(def, ctx, cts.Token));
        }

        [Fact]
        public async Task InvalidateKey_RemovesAllEntriesForKey()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            var cache = new ProviderCache(tickProvider: tickProvider);

            // Manually populate entries by computing
            var def1 = CreateDef(key: "key_a", stalenessTicks: 6000, provider: (_, _) => Task.FromResult<string?>("a"));
            var def2 = CreateDef(key: "key_b", stalenessTicks: 6000, provider: (_, _) => Task.FromResult<string?>("b"));

            var ctx1 = CreateCtx(npcId: "npc-1");
            var ctx2 = CreateCtx(npcId: "npc-2");

            await cache.GetOrComputeAsync(def1, ctx1, CancellationToken.None);
            await cache.GetOrComputeAsync(def1, ctx2, CancellationToken.None);
            await cache.GetOrComputeAsync(def2, ctx1, CancellationToken.None);

            Assert.Equal(3, cache.Count);

            cache.InvalidateKey("key_a");

            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public async Task InvalidateNpc_RemovesAllEntriesForNpc()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            var cache = new ProviderCache(tickProvider: tickProvider);

            var def = CreateDef(key: "shared_key", stalenessTicks: 6000, provider: (ctx, _) =>
                Task.FromResult<string?>($"for_{ctx.NpcId}"));

            var ctx1 = CreateCtx(npcId: "npc-1");
            var ctx2 = CreateCtx(npcId: "npc-2");

            await cache.GetOrComputeAsync(def, ctx1, CancellationToken.None);
            await cache.GetOrComputeAsync(def, ctx2, CancellationToken.None);

            Assert.Equal(2, cache.Count);

            cache.InvalidateNpc("npc-1");

            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public async Task Clear_RemovesAllEntries()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            var cache = new ProviderCache(tickProvider: tickProvider);

            var def = CreateDef(stalenessTicks: 6000, provider: (_, _) => Task.FromResult<string?>("v"));
            var ctx = CreateCtx();

            await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Equal(1, cache.Count);

            cache.Clear();
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public async Task GetOrCompute_ProviderReturnsNull_CachesNull()
        {
            var tickProvider = new ManualTickProvider { CurrentTick = 100 };
            int callCount = 0;
            var def = CreateDef(stalenessTicks: 600, provider: (_, _) =>
            {
                callCount++;
                return Task.FromResult<string?>(null);
            });
            var cache = new ProviderCache(tickProvider: tickProvider);
            var ctx = CreateCtx();

            var result1 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Null(result1);
            Assert.Equal(1, callCount);

            // Within staleness window - should return cached null without calling provider again
            var result2 = await cache.GetOrComputeAsync(def, ctx, CancellationToken.None);
            Assert.Null(result2);
            Assert.Equal(1, callCount);
        }
    }
}
