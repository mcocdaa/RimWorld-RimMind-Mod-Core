using RimMind.Core.Prompt;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PromptSectionTests
    {
        [Fact]
        public void EstimateTokens_EmptyString_ReturnsZero()
        {
            Assert.Equal(0, PromptSection.EstimateTokens(""));
        }

        [Fact]
        public void EstimateTokens_NullString_ReturnsZero()
        {
            Assert.Equal(0, PromptSection.EstimateTokens(null!));
        }

        [Fact]
        public void EstimateTokens_AsciiOnly()
        {
            int tokens = PromptSection.EstimateTokens("abcdefghij");
            Assert.Equal(3, tokens);
        }

        [Fact]
        public void EstimateTokens_CjkOnly()
        {
            int tokens = PromptSection.EstimateTokens("你好世界");
            Assert.Equal(3, tokens);
        }

        [Fact]
        public void EstimateTokens_MixedAsciiAndCjk()
        {
            string text = "Hello你好";
            int tokens = PromptSection.EstimateTokens(text);
            Assert.True(tokens > 0);
        }

        [Fact]
        public void EstimateTokens_SingleCjkChar()
        {
            int tokens = PromptSection.EstimateTokens("你");
            Assert.Equal(1, tokens);
        }

        [Fact]
        public void EstimateTokens_FourCjkChars()
        {
            int tokens = PromptSection.EstimateTokens("你好世界");
            Assert.Equal(3, tokens);
        }

        [Fact]
        public void Constructor_SetsEstimatedTokens()
        {
            var section = new PromptSection("test", "Hello World 你好");
            Assert.True(section.EstimatedTokens > 0);
        }

        [Fact]
        public void IsTrimable_CorePriority_ReturnsFalse()
        {
            var section = new PromptSection("core", "content", PromptSection.PriorityCore);
            Assert.False(section.IsTrimable);
        }

        [Fact]
        public void IsTrimable_AuxiliaryPriority_ReturnsTrue()
        {
            var section = new PromptSection("aux", "content", PromptSection.PriorityAuxiliary);
            Assert.True(section.IsTrimable);
        }

        [Fact]
        public void IsCompressible_WithCompressAndTrimable_ReturnsTrue()
        {
            var section = new PromptSection("hist", "content", PromptSection.PriorityMemory)
            {
                Compress = s => s,
            };
            Assert.True(section.IsCompressible);
        }

        [Fact]
        public void IsCompressible_NoCompress_ReturnsFalse()
        {
            var section = new PromptSection("hist", "content", PromptSection.PriorityMemory);
            Assert.False(section.IsCompressible);
        }

        [Fact]
        public void IsCompressible_CorePriority_ReturnsFalse()
        {
            var section = new PromptSection("core", "content", PromptSection.PriorityCore)
            {
                Compress = s => s,
            };
            Assert.False(section.IsCompressible);
        }

        [Fact]
        public void Clone_CopiesAllFields()
        {
            var original = new PromptSection("tag", "content", 5)
            {
                Compress = s => s,
                LayerTag = "L3",
            };
            original.EstimatedTokens = 42;

            var clone = original.Clone();
            Assert.Equal("tag", clone.Tag);
            Assert.Equal("content", clone.Content);
            Assert.Equal(5, clone.Priority);
            Assert.Equal(42, clone.EstimatedTokens);
            Assert.Equal("L3", clone.LayerTag);
            Assert.NotNull(clone.Compress);
        }

        [Fact]
        public void Clone_ModifyingCloneDoesNotAffectOriginal()
        {
            var original = new PromptSection("tag", "content", 5);
            var clone = original.Clone();
            clone.Content = "modified";

            Assert.Equal("content", original.Content);
        }

        [Fact]
        public void ToString_ContainsTagAndPriority()
        {
            var section = new PromptSection("test", "hello", 3);
            string str = section.ToString();
            Assert.Contains("test", str);
            Assert.Contains("P3", str);
        }

        [Fact]
        public void PriorityConstants_AreOrdered()
        {
            Assert.True(PromptSection.PriorityCore < PromptSection.PriorityCurrentInput);
            Assert.True(PromptSection.PriorityCurrentInput < PromptSection.PriorityKeyState);
            Assert.True(PromptSection.PriorityKeyState < PromptSection.PriorityMemory);
            Assert.True(PromptSection.PriorityMemory < PromptSection.PriorityAuxiliary);
            Assert.True(PromptSection.PriorityAuxiliary < PromptSection.PriorityCustom);
        }
    }
}
