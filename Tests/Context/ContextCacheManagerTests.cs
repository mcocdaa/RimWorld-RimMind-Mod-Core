using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Cache;
using Xunit;

namespace RimMind.Tests.Context
{
    /// <summary>
    /// Unit tests for <see cref="ContextCacheManager"/> L1 block cache thread-safety
    /// guarantees introduced by the unified lock strategy on <c>_l1BlockCache</c>.
    /// Covers Set/TryGet consistency, snapshot isolation, and concurrent access safety.
    /// </summary>
    public class ContextCacheManagerTests
    {
        private static ContextCacheManager CreateManager()
        {
            return new ContextCacheManager(log: null, embedCache: new EmbedCache());
        }

        [Fact]
        public void SetL1BlockCache_Then_TryGetL1BlockCache_ReturnsSameData()
        {
            var manager = CreateManager();
            var blocks = new Dictionary<string, string>
            {
                ["health"] = "80",
                ["mood"] = "happy",
            };

            manager.SetL1BlockCache("npc-1", blocks);

            Assert.True(manager.TryGetL1BlockCache("npc-1", out var retrieved));
            Assert.Equal(2, retrieved.Count);
            Assert.Equal("80", retrieved["health"]);
            Assert.Equal("happy", retrieved["mood"]);
        }

        [Fact]
        public void TryGetL1BlockCache_NonExistentNpc_ReturnsFalse_NullBlocks()
        {
            var manager = CreateManager();

            var result = manager.TryGetL1BlockCache("missing", out var blocks);

            Assert.False(result);
            Assert.Null(blocks);
        }

        [Fact]
        public void TryGetL1BlockCache_ReturnsSnapshot_ModifyingReturnedDoesNotAffectInternal()
        {
            var manager = CreateManager();
            var blocks = new Dictionary<string, string>
            {
                ["health"] = "80",
            };
            manager.SetL1BlockCache("npc-1", blocks);

            Assert.True(manager.TryGetL1BlockCache("npc-1", out var firstSnapshot));
            firstSnapshot["health"] = "tampered";
            firstSnapshot.Add("injected", "x");

            Assert.True(manager.TryGetL1BlockCache("npc-1", out var secondSnapshot));
            Assert.Equal("80", secondSnapshot["health"]);
            Assert.False(secondSnapshot.ContainsKey("injected"));
            Assert.Single(secondSnapshot);
        }

        [Fact]
        public void SetL1BlockCache_ReplacesExisting_TryGetReturnsNewData()
        {
            var manager = CreateManager();
            manager.SetL1BlockCache("npc-1", new Dictionary<string, string> { ["v"] = "1" });

            manager.SetL1BlockCache("npc-1", new Dictionary<string, string> { ["v"] = "2", ["extra"] = "y" });

            Assert.True(manager.TryGetL1BlockCache("npc-1", out var retrieved));
            Assert.Equal("2", retrieved["v"]);
            Assert.True(retrieved.ContainsKey("extra"));
            Assert.Equal(2, retrieved.Count);
        }

        [Fact]
        public void TryGetL1BlockCache_SnapshotIndependentOfLaterSet()
        {
            var manager = CreateManager();
            manager.SetL1BlockCache("npc-1", new Dictionary<string, string> { ["v"] = "1" });

            Assert.True(manager.TryGetL1BlockCache("npc-1", out var snapshot));
            manager.SetL1BlockCache("npc-1", new Dictionary<string, string> { ["v"] = "2" });

            Assert.Equal("1", snapshot["v"]);
            Assert.Single(snapshot);
        }

        [Fact]
        public async Task Concurrent_SetAndTryGet_NoException_DataRemainsConsistent()
        {
            var manager = CreateManager();
            const int iterations = 200;
            const int threadCount = 8;
            var errors = new List<string>();
            var errorLock = new object();

            async Task Worker(int workerId)
            {
                await Task.Yield();
                for (var i = 0; i < iterations; i++)
                {
                    try
                    {
                        var npcId = "npc-" + (i % 4);
                        if ((i + workerId) % 2 == 0)
                        {
                            manager.SetL1BlockCache(npcId, new Dictionary<string, string>
                            {
                                ["k" + i] = "v" + i,
                                ["worker"] = workerId.ToString(),
                            });
                        }
                        else
                        {
                            if (manager.TryGetL1BlockCache(npcId, out var blocks))
                            {
                                // Mutating the returned snapshot must never throw and must not
                                // affect the internal state observed by other threads.
                                blocks["local"] = "mut";
                                Assert.Equal("mut", blocks["local"]);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        lock (errorLock) { errors.Add($"worker={workerId} i={i}: {ex.GetType().Name}"); }
                    }
                }
            }

            var tasks = new Task[threadCount];
            for (var t = 0; t < threadCount; t++)
            {
                tasks[t] = Worker(t);
            }
            await Task.WhenAll(tasks);

            Assert.Empty(errors);

            // After concurrent churn, every present npc entry must be readable and internally consistent.
            for (var n = 0; n < 4; n++)
            {
                if (manager.TryGetL1BlockCache("npc-" + n, out var blocks))
                {
                    Assert.True(blocks.Count >= 1);
                }
            }
        }

        [Fact]
        public void InvalidateLayer_RemovesL1BlockCache_TryGetReturnsFalse()
        {
            var manager = CreateManager();
            manager.SetL1BlockCache("npc-1", new Dictionary<string, string> { ["v"] = "1" });

            manager.InvalidateLayer("npc-1", ContextLayer.L1_Baseline);

            Assert.False(manager.TryGetL1BlockCache("npc-1", out var blocks));
            Assert.Null(blocks);
        }

        [Fact]
        public void Reset_ClearsL1BlockCache_TryGetReturnsFalse()
        {
            var manager = CreateManager();
            manager.SetL1BlockCache("npc-1", new Dictionary<string, string> { ["v"] = "1" });
            manager.SetL1BlockCache("npc-2", new Dictionary<string, string> { ["v"] = "2" });

            manager.Reset();

            Assert.False(manager.TryGetL1BlockCache("npc-1", out _));
            Assert.False(manager.TryGetL1BlockCache("npc-2", out _));
            Assert.Equal(0, manager.GetL1BlockCacheCount());
        }
    }
}
