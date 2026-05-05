using System.Collections.Generic;
using System.Linq;
using RimMind.Core.Prompt;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PromptBudgetTests
    {
        [Fact]
        public void Compose_UnderBudget_ReturnsAllSections()
        {
            var budget = new PromptBudget(totalBudget: 1000, reserveForOutput: 200);
            var sections = new List<PromptSection>
            {
                new PromptSection("sys", "system prompt", PromptSection.PriorityCore),
                new PromptSection("ctx", "context data", PromptSection.PriorityCurrentInput),
            };

            var result = budget.Compose(sections);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Compose_OverBudget_TrimsLowPriorityFirst()
        {
            var budget = new PromptBudget(totalBudget: 30, reserveForOutput: 0);
            var sections = new List<PromptSection>
            {
                new PromptSection("core", new string('a', 80), PromptSection.PriorityCore),
                new PromptSection("aux", new string('b', 80), PromptSection.PriorityAuxiliary),
            };

            var result = budget.Compose(sections);
            Assert.Single(result);
            Assert.Equal("core", result[0].Tag);
        }

        [Fact]
        public void Compose_DoesNotTrimCorePriority()
        {
            var budget = new PromptBudget(totalBudget: 50, reserveForOutput: 0);
            var sections = new List<PromptSection>
            {
                new PromptSection("core", new string('a', 200), PromptSection.PriorityCore),
            };

            var result = budget.Compose(sections);
            Assert.Single(result);
            Assert.Equal("core", result[0].Tag);
        }

        [Fact]
        public void Compose_CompressesBeforeTrimming()
        {
            var budget = new PromptBudget(totalBudget: 30, reserveForOutput: 0);
            var sections = new List<PromptSection>
            {
                new PromptSection("core", new string('a', 80), PromptSection.PriorityCore),
                new PromptSection("hist", new string('h', 80), PromptSection.PriorityMemory)
                {
                    Compress = s => s.Substring(0, 20),
                },
            };

            var result = budget.Compose(sections);
            Assert.Equal(2, result.Count);
            Assert.True(result[1].Content.Length <= 20);
        }

        [Fact]
        public void Compose_NullSections_ReturnsNull()
        {
            var budget = new PromptBudget();
            var result = budget.Compose(null!);
            Assert.Null(result);
        }

        [Fact]
        public void Compose_EmptySections_ReturnsEmpty()
        {
            var budget = new PromptBudget();
            var result = budget.Compose(new List<PromptSection>());
            Assert.Empty(result);
        }

        [Fact]
        public void ComposeToString_JoinsWithDoubleNewline()
        {
            var budget = new PromptBudget(totalBudget: 10000, reserveForOutput: 1000);
            var sections = new List<PromptSection>
            {
                new PromptSection("a", "hello"),
                new PromptSection("b", "world"),
            };

            string result = budget.ComposeToString(sections);
            Assert.Equal("hello\n\nworld", result);
        }

        [Fact]
        public void AvailableForInput_CalculatedCorrectly()
        {
            var budget = new PromptBudget(totalBudget: 4000, reserveForOutput: 800);
            Assert.Equal(3200, budget.AvailableForInput);
        }

        [Fact]
        public void Compose_HigherPriorityTrimmedLast()
        {
            var budget = new PromptBudget(totalBudget: 30, reserveForOutput: 0);
            var sections = new List<PromptSection>
            {
                new PromptSection("core", new string('a', 80), PromptSection.PriorityCore),
                new PromptSection("state", new string('s', 80), PromptSection.PriorityKeyState),
                new PromptSection("aux", new string('x', 80), PromptSection.PriorityAuxiliary),
            };

            var result = budget.Compose(sections);
            Assert.Contains(result, s => s.Tag == "core");
            Assert.DoesNotContain(result, s => s.Tag == "aux");
        }
    }
}
