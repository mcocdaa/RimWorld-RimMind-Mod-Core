using System.Collections.Generic;
using RimMind.Core.Agent;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PerceptionPipelineTests
    {
        private static PerceptionBufferEntry MakeEntry(string type, float importance, int timestamp = 0)
        {
            return new PerceptionBufferEntry
            {
                PerceptionType = type,
                Content = $"{type}-content",
                Importance = importance,
                Timestamp = timestamp,
                PawnId = 1,
            };
        }

        [Fact]
        public void Buffer_AddAndFlush()
        {
            var buffer = new PerceptionBuffer(capacity: 5);
            buffer.Add(MakeEntry("raid", 0.8f));
            buffer.Add(MakeEntry("damage", 0.5f));

            var entries = buffer.Flush();
            Assert.Equal(2, entries.Count);
        }

        [Fact]
        public void Buffer_OverCapacity_DropsOldest()
        {
            var buffer = new PerceptionBuffer(capacity: 2);
            buffer.Add(MakeEntry("first", 0.5f, 1));
            buffer.Add(MakeEntry("second", 0.5f, 2));
            buffer.Add(MakeEntry("third", 0.5f, 3));

            var entries = buffer.Flush();
            Assert.Equal(2, entries.Count);
            Assert.Equal("second", entries[0].PerceptionType);
            Assert.Equal("third", entries[1].PerceptionType);
        }

        [Fact]
        public void Buffer_FlushClearsBuffer()
        {
            var buffer = new PerceptionBuffer();
            buffer.Add(MakeEntry("test", 0.5f));
            buffer.Flush();

            var entries = buffer.Flush();
            Assert.Empty(entries);
        }

        [Fact]
        public void Buffer_ClearRemovesAll()
        {
            var buffer = new PerceptionBuffer();
            buffer.Add(MakeEntry("test", 0.5f));
            buffer.Clear();

            var entries = buffer.Flush();
            Assert.Empty(entries);
        }

        [Fact]
        public void PriorityFilter_FiltersBelowThreshold()
        {
            var filter = new PriorityFilter(0.3f);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry("high", 0.8f),
                MakeEntry("low", 0.1f),
                MakeEntry("mid", 0.5f),
            };

            var result = filter.Filter(entries);
            Assert.Equal(2, result.Count);
            Assert.All(result, e => Assert.True(e.Importance >= 0.3f));
        }

        [Fact]
        public void PriorityFilter_AllAboveThreshold()
        {
            var filter = new PriorityFilter(0.1f);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry("a", 0.5f),
                MakeEntry("b", 0.8f),
            };

            var result = filter.Filter(entries);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void PriorityFilter_AllBelowThreshold()
        {
            var filter = new PriorityFilter(0.9f);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry("a", 0.1f),
                MakeEntry("b", 0.2f),
            };

            var result = filter.Filter(entries);
            Assert.Empty(result);
        }

        [Fact]
        public void CooldownFilter_BlocksSameTypeWithinCooldown()
        {
            var filter = new CooldownFilter(cooldownTicks: 100);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry("raid", 0.8f, 1000),
                MakeEntry("raid", 0.8f, 1050),
                MakeEntry("damage", 0.5f, 1050),
            };

            var result = filter.Filter(entries);
            Assert.Equal(2, result.Count);
            Assert.Equal("raid", result[0].PerceptionType);
            Assert.Equal("damage", result[1].PerceptionType);
        }

        [Fact]
        public void CooldownFilter_AllowsSameTypeAfterCooldown()
        {
            var filter = new CooldownFilter(cooldownTicks: 100);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry("raid", 0.8f, 1000),
                MakeEntry("raid", 0.8f, 1200),
            };

            var result = filter.Filter(entries);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void CooldownFilter_ResetClearsState()
        {
            var filter = new CooldownFilter(cooldownTicks: 100);
            var first = new List<PerceptionBufferEntry>
            {
                MakeEntry("raid", 0.8f, 1000),
            };
            filter.Filter(first);
            filter.Reset();

            var second = new List<PerceptionBufferEntry>
            {
                MakeEntry("raid", 0.8f, 1010),
            };
            var result = filter.Filter(second);
            Assert.Single(result);
        }

        [Fact]
        public void Pipeline_ChainsFilters()
        {
            var pipeline = new PerceptionPipeline();
            pipeline.AddFilter(new PriorityFilter(0.3f));
            pipeline.AddFilter(new CooldownFilter(cooldownTicks: 100));

            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry("raid", 0.8f, 1000),
                MakeEntry("raid", 0.7f, 1050),
                MakeEntry("chitchat", 0.1f, 1000),
                MakeEntry("damage", 0.5f, 1050),
            };

            var result = pipeline.Process(entries);
            Assert.Equal(2, result.Count);
            Assert.Equal("raid", result[0].PerceptionType);
            Assert.Equal("damage", result[1].PerceptionType);
        }

        [Fact]
        public void Pipeline_NoFilters_ReturnsAll()
        {
            var pipeline = new PerceptionPipeline();
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry("a", 0.1f),
                MakeEntry("b", 0.9f),
            };

            var result = pipeline.Process(entries);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void DedupKey_Format()
        {
            var entry = new PerceptionBufferEntry
            {
                PerceptionType = "raid",
                Content = "enemy_spotted",
            };
            Assert.Equal("raid:enemy_spotted", entry.DedupKey);
        }
    }
}
