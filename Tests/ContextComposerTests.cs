using System.Collections.Generic;
using RimMind.Core.Prompt;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ContextComposerTests
    {
        [Fact]
        public void Reorder_SortsByPriority()
        {
            var sections = new List<PromptSection>
            {
                new PromptSection("aux", "auxiliary", PromptSection.PriorityAuxiliary),
                new PromptSection("core", "core", PromptSection.PriorityCore),
                new PromptSection("mem", "memory", PromptSection.PriorityMemory),
            };

            var result = ContextComposer.Reorder(sections);
            Assert.Equal("core", result[0].Tag);
            Assert.Equal("mem", result[1].Tag);
            Assert.Equal("aux", result[2].Tag);
        }

        [Fact]
        public void Reorder_SamePriority_SortsByTagOrdinal()
        {
            var sections = new List<PromptSection>
            {
                new PromptSection("z_tag", "z", 5),
                new PromptSection("a_tag", "a", 5),
            };

            var result = ContextComposer.Reorder(sections);
            Assert.Equal("a_tag", result[0].Tag);
            Assert.Equal("z_tag", result[1].Tag);
        }

        [Fact]
        public void Reorder_NullInput_ReturnsNull()
        {
            var result = ContextComposer.Reorder(null!);
            Assert.Null(result);
        }

        [Fact]
        public void Reorder_SingleElement_ReturnsSame()
        {
            var sections = new List<PromptSection>
            {
                new PromptSection("only", "content", 5),
            };
            var result = ContextComposer.Reorder(sections);
            Assert.Single(result);
        }

        [Fact]
        public void BuildFromSections_ConcatenatesContent()
        {
            var sections = new List<PromptSection>
            {
                new PromptSection("a", "hello", 0),
                new PromptSection("b", "world", 1),
            };

            string result = ContextComposer.BuildFromSections(sections);
            Assert.Contains("hello", result);
            Assert.Contains("world", result);
            Assert.True(result.IndexOf("hello") < result.IndexOf("world"));
        }

        [Fact]
        public void BuildFromSections_SingleSection_NoNewline()
        {
            var sections = new List<PromptSection>
            {
                new PromptSection("a", "solo", 0),
            };

            string result = ContextComposer.BuildFromSections(sections);
            Assert.Equal("solo", result);
        }

        [Fact]
        public void CompressHistory_UnderMaxLines_ReturnsUnchanged()
        {
            string history = "line1\nline2\nline3";
            string result = ContextComposer.CompressHistory(history, maxLines: 5);
            Assert.Equal(history, result);
        }

        [Fact]
        public void CompressHistory_OverMaxLines_TruncatesFromTop()
        {
            string history = "line1\nline2\nline3\nline4\nline5\nline6\nline7";
            string result = ContextComposer.CompressHistory(history, maxLines: 3);
            Assert.Contains("line5", result);
            Assert.Contains("line6", result);
            Assert.Contains("line7", result);
            Assert.DoesNotContain("line1", result);
        }

        [Fact]
        public void CompressHistory_WithSummaryLine_PrependsSummary()
        {
            string history = "line1\nline2\nline3\nline4\nline5";
            string result = ContextComposer.CompressHistory(history, maxLines: 2, summaryLine: "[earlier omitted]");
            Assert.StartsWith("[earlier omitted]", result);
            Assert.Contains("line4", result);
            Assert.Contains("line5", result);
        }

        [Fact]
        public void CompressHistory_EmptyInput_ReturnsEmpty()
        {
            string result = ContextComposer.CompressHistory("", maxLines: 3);
            Assert.Equal("", result);
        }

        [Fact]
        public void CompressHistory_NullInput_ReturnsNull()
        {
            string? result = ContextComposer.CompressHistory(null!, maxLines: 3);
            Assert.Null(result);
        }

        [Fact]
        public void CompressHistory_ExactlyMaxLines_ReturnsUnchanged()
        {
            string history = "line1\nline2\nline3";
            string result = ContextComposer.CompressHistory(history, maxLines: 3);
            Assert.Equal(history, result);
        }
    }
}
