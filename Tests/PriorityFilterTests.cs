using System.Collections.Generic;
using RimMind.Core.Agent;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PriorityFilterTests
    {
        private static PerceptionBufferEntry MakeEntry(float importance)
        {
            return new PerceptionBufferEntry { Importance = importance, PerceptionType = "test", Content = "data" };
        }

        [Fact]
        public void Filter_DefaultThreshold_FiltersLowImportance()
        {
            var filter = new PriorityFilter();
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry(0.1f),
                MakeEntry(0.3f),
                MakeEntry(0.5f),
            };

            var result = filter.Filter(entries);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Filter_CustomThreshold()
        {
            var filter = new PriorityFilter(0.5f);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry(0.3f),
                MakeEntry(0.5f),
                MakeEntry(0.7f),
            };

            var result = filter.Filter(entries);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Filter_ExactlyAtThreshold_Included()
        {
            var filter = new PriorityFilter(0.2f);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry(0.2f),
            };

            var result = filter.Filter(entries);
            Assert.Single(result);
        }

        [Fact]
        public void Filter_EmptyList_ReturnsEmpty()
        {
            var filter = new PriorityFilter();
            var result = filter.Filter(new List<PerceptionBufferEntry>());
            Assert.Empty(result);
        }

        [Fact]
        public void Filter_AllBelowThreshold_ReturnsEmpty()
        {
            var filter = new PriorityFilter(0.8f);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry(0.1f),
                MakeEntry(0.3f),
                MakeEntry(0.5f),
            };

            var result = filter.Filter(entries);
            Assert.Empty(result);
        }

        [Fact]
        public void Filter_AllAboveThreshold_ReturnsAll()
        {
            var filter = new PriorityFilter(0.1f);
            var entries = new List<PerceptionBufferEntry>
            {
                MakeEntry(0.5f),
                MakeEntry(0.7f),
                MakeEntry(0.9f),
            };

            var result = filter.Filter(entries);
            Assert.Equal(3, result.Count);
        }
    }
}
