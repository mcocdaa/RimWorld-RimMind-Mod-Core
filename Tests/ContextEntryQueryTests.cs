using System.Collections.Generic;
using RimMind.Core.Context;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ContextEntryQueryTests
    {
        [Fact]
        public void ExtractHour_WithTimeMetadata_ReturnsHour()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("header"),
                new ContextEntry("14:00")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "time",
                        ["hour"] = "14",
                        ["day"] = "42"
                    }
                }
            };
            int result = ContextEntryQuery.ExtractHour(entries);
            Assert.Equal(14, result);
        }

        [Fact]
        public void ExtractHour_NoTimeMetadata_ReturnsDefault()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("header")
            };
            int result = ContextEntryQuery.ExtractHour(entries);
            Assert.Equal(12, result);
        }

        [Fact]
        public void ExtractHour_EmptyEntries_ReturnsDefault()
        {
            var entries = new List<ContextEntry>();
            int result = ContextEntryQuery.ExtractHour(entries);
            Assert.Equal(12, result);
        }

        [Fact]
        public void ExtractHour_InvalidHourValue_ReturnsDefault()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("time")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "time",
                        ["hour"] = "not_a_number"
                    }
                }
            };
            int result = ContextEntryQuery.ExtractHour(entries);
            Assert.Equal(12, result);
        }

        [Fact]
        public void ExtractHour_NullMetadata_ReturnsDefault()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("time") { Metadata = null }
            };
            int result = ContextEntryQuery.ExtractHour(entries);
            Assert.Equal(12, result);
        }

        [Fact]
        public void ExtractColonistCount_WithMetadata_ReturnsCount()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("header"),
                new ContextEntry("3 colonists")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "colonistCount",
                        ["count"] = "3"
                    }
                }
            };
            int result = ContextEntryQuery.ExtractColonistCount(entries);
            Assert.Equal(3, result);
        }

        [Fact]
        public void ExtractColonistCount_NoMetadata_ReturnsDefault()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("header")
            };
            int result = ContextEntryQuery.ExtractColonistCount(entries);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ExtractColonistCount_EmptyEntries_ReturnsDefault()
        {
            var entries = new List<ContextEntry>();
            int result = ContextEntryQuery.ExtractColonistCount(entries);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ExtractColonistCount_InvalidCountValue_ReturnsDefault()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("colonists")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "colonistCount",
                        ["count"] = "abc"
                    }
                }
            };
            int result = ContextEntryQuery.ExtractColonistCount(entries);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ExtractHour_MultipleEntries_FindsCorrectOne()
        {
            var entries = new List<ContextEntry>
            {
                new ContextEntry("header"),
                new ContextEntry("other")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "colonistCount",
                        ["count"] = "5"
                    }
                },
                new ContextEntry("08:00")
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["key"] = "time",
                        ["hour"] = "8",
                        ["day"] = "10"
                    }
                }
            };
            int result = ContextEntryQuery.ExtractHour(entries);
            Assert.Equal(8, result);
        }

        [Fact]
        public void ContextEntry_Metadata_DefaultNull()
        {
            var entry = new ContextEntry("content");
            Assert.Null(entry.Metadata);
        }

        [Fact]
        public void ContextEntry_Metadata_SetViaConstructor()
        {
            var meta = new Dictionary<string, string> { ["key"] = "time" };
            var entry = new ContextEntry("content", metadata: meta);
            Assert.NotNull(entry.Metadata);
            Assert.Equal("time", entry.Metadata["key"]);
        }

        [Fact]
        public void ContextEntry_Metadata_SetViaProperty()
        {
            var entry = new ContextEntry("content")
            {
                Metadata = new Dictionary<string, string> { ["key"] = "test" }
            };
            Assert.NotNull(entry.Metadata);
            Assert.Equal("test", entry.Metadata["key"]);
        }

        [Fact]
        public void ContextSnapshot_AllEntries_DefaultEmpty()
        {
            var snapshot = new ContextSnapshot();
            Assert.NotNull(snapshot.AllEntries);
            Assert.Empty(snapshot.AllEntries);
        }
    }
}
