using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    /// <summary>
    /// Tests for N3.2: Task.WhenAll parallel Provider execution pattern.
    /// Verifies the parallel layer build and merge logic, including fault tolerance.
    /// </summary>
    public class ParallelLayerBuildTests
    {
        /// <summary>
        /// Simulates the LayerBuildResult record from ContextOrchestrator.
        /// </summary>
        private readonly record struct LayerBuildResult(
            List<ChatMessage> Messages,
            long LatencyMs,
            int TokenCount,
            string LayerTag,
            List<ContextEntry>? ExtraEntries
        );

        [Fact]
        public async Task TaskWhenAll_AllSucceed_AllResultsMerged()
        {
            var results = await Task.WhenAll(
                Task.Run(() => BuildLayer("L0", "system", "L0 content")),
                Task.Run(() => BuildLayer("L1", "system", "L1 content")),
                Task.Run(() => BuildLayer("L2", "system", "L2 content"))
            );

            var messages = new List<ChatMessage>();
            foreach (var r in results)
            {
                messages.AddRange(r.Messages);
            }

            Assert.Equal(3, messages.Count);
            Assert.Contains(messages, m => m.LayerTag == "L0");
            Assert.Contains(messages, m => m.LayerTag == "L1");
            Assert.Contains(messages, m => m.LayerTag == "L2");
        }

        [Fact]
        public async Task TaskWhenAll_OneFails_OthersSucceed()
        {
            var results = await Task.WhenAll(
                Task.Run(() => BuildLayerSafe("L0", "system", "L0 content")),
                Task.Run(() => BuildLayerSafeFailing("L1")),
                Task.Run(() => BuildLayerSafe("L2", "system", "L2 content"))
            );

            var messages = new List<ChatMessage>();
            foreach (var r in results)
            {
                if (r == null) continue;
                messages.AddRange(r.Value.Messages);
            }

            // L1 failed, so only L0 and L2 messages should be present
            Assert.Equal(2, messages.Count);
            Assert.Contains(messages, m => m.LayerTag == "L0");
            Assert.Contains(messages, m => m.LayerTag == "L2");
            Assert.DoesNotContain(messages, m => m.LayerTag == "L1");
        }

        [Fact]
        public async Task TaskWhenAll_AllFail_NoMessages()
        {
            var results = await Task.WhenAll(
                Task.Run(() => BuildLayerSafeFailing("L0")),
                Task.Run(() => BuildLayerSafeFailing("L1")),
                Task.Run(() => BuildLayerSafeFailing("L2"))
            );

            var messages = new List<ChatMessage>();
            foreach (var r in results)
            {
                if (r == null) continue;
                messages.AddRange(r.Value.Messages);
            }

            Assert.Empty(messages);
        }

        [Fact]
        public async Task TaskWhenAll_MergeTokenCounts_CorrectTotals()
        {
            var results = await Task.WhenAll(
                Task.Run(() => BuildLayer("L0", "system", "short")),
                Task.Run(() => BuildLayer("L2", "system", "a bit longer content"))
            );

            int l0Tokens = 0, l2Tokens = 0;
            foreach (var r in results)
            {
                switch (r.LayerTag)
                {
                    case "L0": l0Tokens = r.TokenCount; break;
                    case "L2": l2Tokens = r.TokenCount; break;
                }
            }

            Assert.True(l0Tokens > 0, "L0 tokens should be positive");
            Assert.True(l2Tokens > 0, "L2 tokens should be positive");
            Assert.True(l2Tokens > l0Tokens, "L2 with more content should have more tokens");
        }

        [Fact]
        public async Task TaskWhenAll_LatencyRecorded_ForAllLayers()
        {
            var results = await Task.WhenAll(
                Task.Run(() => BuildLayer("L0", "system", "content")),
                Task.Run(() => BuildLayer("L5", "system", "content"))
            );

            foreach (var r in results)
            {
                Assert.True(r.LatencyMs >= 0, $"Latency for {r.LayerTag} should be non-negative");
            }
        }

        [Fact]
        public async Task TaskWhenAll_CancellationRequested_ThrowsOperationCanceled()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.WhenAll(
                    Task.Run(() => BuildLayer("L0", "system", "content"), cts.Token)
                );
            });
        }

        [Fact]
        public async Task TaskWhenAll_ExtraEntries_MergedCorrectly()
        {
            var extraEntries = new List<ContextEntry>
            {
                new ContextEntry("map_structure data", tag: "map_structure")
            };

            var result = await Task.Run(() => new LayerBuildResult(
                new List<ChatMessage> { new ChatMessage { Role = "system", Content = "L1" } },
                10, 5, "L1", extraEntries
            ));

            Assert.NotNull(result.ExtraEntries);
            Assert.Single(result.ExtraEntries);
            Assert.Equal("map_structure", result.ExtraEntries[0].Tag);
        }

        [Fact]
        public async Task TaskWhenAll_FiveLayers_AllComplete()
        {
            var results = await Task.WhenAll(
                Task.Run(() => BuildLayer("L0", "system", "L0")),
                Task.Run(() => BuildLayer("L1", "system", "L1")),
                Task.Run(() => BuildLayer("L2", "system", "L2")),
                Task.Run(() => BuildLayer("L3", "system", "L3")),
                Task.Run(() => BuildLayer("L5", "system", "L5"))
            );

            Assert.Equal(5, results.Length);
            var tags = results.Select(r => r.LayerTag).ToHashSet();
            Assert.Contains("L0", tags);
            Assert.Contains("L1", tags);
            Assert.Contains("L2", tags);
            Assert.Contains("L3", tags);
            Assert.Contains("L5", tags);
        }

        // --- Helpers ---

        private static LayerBuildResult BuildLayer(string tag, string role, string content)
        {
            long start = DateTime.Now.Ticks;
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = role, Content = content, LayerTag = tag }
            };
            int tokens = EstimateTokens(content);
            long latencyMs = (DateTime.Now.Ticks - start) / TimeSpan.TicksPerMillisecond;
            return new LayerBuildResult(messages, latencyMs, tokens, tag, null);
        }

        private static LayerBuildResult? BuildLayerSafe(string tag, string role, string content)
        {
            try
            {
                return BuildLayer(tag, role, content);
            }
            catch
            {
                return null;
            }
        }

        private static LayerBuildResult? BuildLayerSafeFailing(string tag)
        {
            try
            {
                throw new InvalidOperationException($"Simulated failure for {tag}");
            }
            catch
            {
                return null;
            }
        }

        private static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return text.Length / 4 + 1;
        }
    }
}
