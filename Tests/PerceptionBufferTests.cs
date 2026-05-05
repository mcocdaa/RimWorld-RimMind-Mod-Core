using RimMind.Core.Agent;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PerceptionBufferTests
    {
        private static PerceptionBufferEntry MakeEntry(string type = "test", string content = "data", float importance = 0.5f)
        {
            return new PerceptionBufferEntry { PerceptionType = type, Content = content, Importance = importance };
        }

        [Fact]
        public void Add_WithinCapacity_EntryStored()
        {
            var buffer = new PerceptionBuffer(5);
            buffer.Add(MakeEntry("a", "hello"));
            Assert.Single(buffer.Entries);
        }

        [Fact]
        public void Add_ExceedsCapacity_OldestEvicted()
        {
            var buffer = new PerceptionBuffer(3);
            buffer.Add(MakeEntry("a", "1"));
            buffer.Add(MakeEntry("b", "2"));
            buffer.Add(MakeEntry("c", "3"));
            buffer.Add(MakeEntry("d", "4"));

            Assert.Equal(3, buffer.Entries.Count);
            Assert.Equal("2", buffer.Entries[0].Content);
            Assert.Equal("4", buffer.Entries[2].Content);
        }

        [Fact]
        public void Flush_ReturnsAllEntriesAndClears()
        {
            var buffer = new PerceptionBuffer(10);
            buffer.Add(MakeEntry("a", "1"));
            buffer.Add(MakeEntry("b", "2"));

            var flushed = buffer.Flush();
            Assert.Equal(2, flushed.Count);
            Assert.Empty(buffer.Entries);
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            var buffer = new PerceptionBuffer(10);
            buffer.Add(MakeEntry("a", "1"));
            buffer.Add(MakeEntry("b", "2"));
            buffer.Clear();
            Assert.Empty(buffer.Entries);
        }

        [Fact]
        public void Capacity_DefaultIs20()
        {
            var buffer = new PerceptionBuffer();
            Assert.Equal(20, buffer.Capacity);
        }

        [Fact]
        public void Entries_IsSnapshot_NotLiveReference()
        {
            var buffer = new PerceptionBuffer(10);
            buffer.Add(MakeEntry("a", "1"));
            var entries = buffer.Entries;
            buffer.Add(MakeEntry("b", "2"));
            Assert.Single(entries);
            Assert.Equal(2, buffer.Entries.Count);
        }

        [Fact]
        public void DedupKey_CombinesTypeAndContent()
        {
            var entry = MakeEntry("raid", "enemy_spotted");
            Assert.Equal("raid:enemy_spotted", entry.DedupKey);
        }

        [Fact]
        public void Add_ManyEntries_OnlyKeepsCapacity()
        {
            var buffer = new PerceptionBuffer(5);
            for (int i = 0; i < 100; i++)
                buffer.Add(MakeEntry("t", i.ToString()));

            Assert.Equal(5, buffer.Entries.Count);
            Assert.Equal("95", buffer.Entries[0].Content);
            Assert.Equal("99", buffer.Entries[4].Content);
        }
    }
}
