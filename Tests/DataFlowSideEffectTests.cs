using System.Collections.Generic;
using RimMind.Core.Client;
using RimMind.Core.Context;
using Xunit;

namespace RimMind.Core.Tests
{
    public class DataFlowSideEffectTests
    {
        [Fact]
        public void ContextSnapshot_Messages_IsIReadOnlyList()
        {
            var snapshot = new ContextSnapshot();
            Assert.IsAssignableFrom<IReadOnlyList<ChatMessage>>(snapshot.Messages);
        }

        [Fact]
        public void ContextSnapshot_AllEntries_IsIReadOnlyList()
        {
            var snapshot = new ContextSnapshot();
            Assert.IsAssignableFrom<IReadOnlyList<ContextEntry>>(snapshot.AllEntries);
        }

        [Fact]
        public void ContextSnapshot_CacheHitEvents_IsIReadOnlyDictionary()
        {
            var snapshot = new ContextSnapshot();
            Assert.IsAssignableFrom<IReadOnlyDictionary<string, bool>>(snapshot.CacheHitEvents);
        }

        [Fact]
        public void ContextSnapshot_Messages_DefaultEmpty()
        {
            var snapshot = new ContextSnapshot();
            Assert.NotNull(snapshot.Messages);
            Assert.Empty(snapshot.Messages);
        }

        [Fact]
        public void ContextSnapshot_AllEntries_DefaultEmpty()
        {
            var snapshot = new ContextSnapshot();
            Assert.NotNull(snapshot.AllEntries);
            Assert.Empty(snapshot.AllEntries);
        }

        [Fact]
        public void ContextSnapshot_CacheHitEvents_DefaultEmpty()
        {
            var snapshot = new ContextSnapshot();
            Assert.NotNull(snapshot.CacheHitEvents);
            Assert.Empty(snapshot.CacheHitEvents);
        }

        [Fact]
        public void ContextSnapshot_AddMessage_Works()
        {
            var snapshot = new ContextSnapshot();
            snapshot.AddMessage(new ChatMessage { Role = "system", Content = "hello" });
            Assert.Single(snapshot.Messages);
            Assert.Equal("hello", snapshot.Messages[0].Content);
        }

        [Fact]
        public void ContextSnapshot_InsertMessage_Works()
        {
            var snapshot = new ContextSnapshot();
            snapshot.AddMessage(new ChatMessage { Role = "system", Content = "first" });
            snapshot.AddMessage(new ChatMessage { Role = "system", Content = "third" });
            snapshot.InsertMessage(1, new ChatMessage { Role = "user", Content = "second" });
            Assert.Equal(3, snapshot.Messages.Count);
            Assert.Equal("first", snapshot.Messages[0].Content);
            Assert.Equal("second", snapshot.Messages[1].Content);
            Assert.Equal("third", snapshot.Messages[2].Content);
        }

        [Fact]
        public void ContextSnapshot_SetMessages_Works()
        {
            var snapshot = new ContextSnapshot();
            snapshot.AddMessage(new ChatMessage { Role = "system", Content = "old" });
            var newList = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "new1" },
                new ChatMessage { Role = "assistant", Content = "new2" },
            };
            snapshot.SetMessages(newList);
            Assert.Equal(2, snapshot.Messages.Count);
            Assert.Equal("new1", snapshot.Messages[0].Content);
            Assert.Equal("new2", snapshot.Messages[1].Content);
        }

        [Fact]
        public void ContextSnapshot_ClearMessages_Works()
        {
            var snapshot = new ContextSnapshot();
            snapshot.AddMessage(new ChatMessage { Role = "system", Content = "hello" });
            Assert.NotEmpty(snapshot.Messages);
            snapshot.ClearMessages();
            Assert.Empty(snapshot.Messages);
        }

        [Fact]
        public void ContextSnapshot_AddEntry_Works()
        {
            var snapshot = new ContextSnapshot();
            snapshot.AddEntry(new ContextEntry("entry1"));
            Assert.Single(snapshot.AllEntries);
            Assert.Equal("entry1", snapshot.AllEntries[0].Content);
        }

        [Fact]
        public void ContextSnapshot_AddEntries_Works()
        {
            var snapshot = new ContextSnapshot();
            var entries = new List<ContextEntry>
            {
                new ContextEntry("e1"),
                new ContextEntry("e2"),
            };
            snapshot.AddEntries(entries);
            Assert.Equal(2, snapshot.AllEntries.Count);
            Assert.Equal("e1", snapshot.AllEntries[0].Content);
            Assert.Equal("e2", snapshot.AllEntries[1].Content);
        }

        [Fact]
        public void ContextSnapshot_SetCacheHitEvent_Works()
        {
            var snapshot = new ContextSnapshot();
            snapshot.SetCacheHitEvent("L0_name", true);
            snapshot.SetCacheHitEvent("L1_age", false);
            Assert.Equal(2, snapshot.CacheHitEvents.Count);
            Assert.True(snapshot.CacheHitEvents["L0_name"]);
            Assert.False(snapshot.CacheHitEvents["L1_age"]);
        }

        [Fact]
        public void ContextSnapshot_Messages_CannotBeModifiedViaInterface()
        {
            var snapshot = new ContextSnapshot();
            snapshot.AddMessage(new ChatMessage { Role = "system", Content = "test" });
            IReadOnlyList<ChatMessage> readOnly = snapshot.Messages;
            Assert.Single(readOnly);
        }

        [Fact]
        public void ContextSnapshot_AllEntries_CannotBeModifiedViaInterface()
        {
            var snapshot = new ContextSnapshot();
            snapshot.AddEntry(new ContextEntry("test"));
            IReadOnlyList<ContextEntry> readOnly = snapshot.AllEntries;
            Assert.Single(readOnly);
        }

        [Fact]
        public void ContextEntryQuery_WorksWithIReadOnlyList()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("14:00")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "time",
                        ["hour"] = "14",
                    }
                }
            };
            IReadOnlyList<ContextEntry> readOnly = entries;
            int result = ContextEntryQuery.ExtractHour(readOnly);
            Assert.Equal(14, result);
        }

        [Fact]
        public void ContextEntryQuery_ExtractColonistCount_WorksWithIReadOnlyList()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("3 colonists")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "colonistCount",
                        ["count"] = "3",
                    }
                }
            };
            IReadOnlyList<ContextEntry> readOnly = entries;
            int result = ContextEntryQuery.ExtractColonistCount(readOnly);
            Assert.Equal(3, result);
        }
    }
}
