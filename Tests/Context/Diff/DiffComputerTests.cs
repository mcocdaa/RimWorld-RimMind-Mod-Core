using System.Collections.Generic;
using RimMind.Application.Features.Context.Diff;
using Xunit;

namespace RimMind.Tests.Context.Diff
{
    public class DiffComputerTests
    {
        private readonly DiffComputer _computer = new();

        [Fact]
        public void Compute_NoChanges_ReturnsEmptyList()
        {
            var oldValues = new Dictionary<string, string> { ["Health"] = "100", ["Mood"] = "80" };
            var newValues = new Dictionary<string, string> { ["Health"] = "100", ["Mood"] = "80" };

            var diffs = _computer.Compute(oldValues, newValues, ContextLayer.L1_Baseline);

            Assert.Empty(diffs);
        }

        [Fact]
        public void Compute_ChangedValue_ReturnsOneDiff()
        {
            var oldValues = new Dictionary<string, string> { ["Health"] = "100" };
            var newValues = new Dictionary<string, string> { ["Health"] = "75" };

            var diffs = _computer.Compute(oldValues, newValues, ContextLayer.L1_Baseline);

            Assert.Single(diffs);
            Assert.Equal("Health", diffs[0].Key);
            Assert.Equal("100", diffs[0].OldValue);
            Assert.Equal("75", diffs[0].NewValue);
            Assert.Equal(ContextLayer.L1_Baseline, diffs[0].Layer);
        }

        [Fact]
        public void Compute_NewKey_ReturnsDiffWithEmptyOldValue()
        {
            var oldValues = new Dictionary<string, string> { ["Health"] = "100" };
            var newValues = new Dictionary<string, string> { ["Health"] = "100", ["Mood"] = "80" };

            var diffs = _computer.Compute(oldValues, newValues, ContextLayer.L3_State);

            Assert.Single(diffs);
            Assert.Equal("Mood", diffs[0].Key);
            Assert.Equal(string.Empty, diffs[0].OldValue);
            Assert.Equal("80", diffs[0].NewValue);
            Assert.Equal(ContextLayer.L3_State, diffs[0].Layer);
        }

        [Fact]
        public void Compute_RemovedKey_ReturnsDiffWithEmptyNewValue()
        {
            var oldValues = new Dictionary<string, string> { ["Health"] = "100", ["Mood"] = "80" };
            var newValues = new Dictionary<string, string> { ["Health"] = "100" };

            var diffs = _computer.Compute(oldValues, newValues, ContextLayer.L2_Environment);

            Assert.Single(diffs);
            Assert.Equal("Mood", diffs[0].Key);
            Assert.Equal("80", diffs[0].OldValue);
            Assert.Equal(string.Empty, diffs[0].NewValue);
            Assert.Equal(ContextLayer.L2_Environment, diffs[0].Layer);
        }

        [Fact]
        public void Compute_NullOldValues_ReturnsEmptyList()
        {
            var newValues = new Dictionary<string, string> { ["Health"] = "100" };

            var diffs = _computer.Compute(null!, newValues, ContextLayer.L1_Baseline);

            Assert.Empty(diffs);
        }

        [Fact]
        public void Compute_NullNewValues_ReturnsEmptyList()
        {
            var oldValues = new Dictionary<string, string> { ["Health"] = "100" };

            var diffs = _computer.Compute(oldValues, null!, ContextLayer.L1_Baseline);

            Assert.Empty(diffs);
        }

        [Fact]
        public void Compute_BothNull_ReturnsEmptyList()
        {
            var diffs = _computer.Compute(null!, null!, ContextLayer.L1_Baseline);

            Assert.Empty(diffs);
        }

        [Fact]
        public void Compute_MultipleChanges_ReturnsAllDiffs()
        {
            var oldValues = new Dictionary<string, string> { ["Health"] = "100", ["Mood"] = "80", ["Hunger"] = "50" };
            var newValues = new Dictionary<string, string> { ["Health"] = "75", ["Mood"] = "80", ["Energy"] = "90" };

            var diffs = _computer.Compute(oldValues, newValues, ContextLayer.L1_Baseline);

            Assert.Equal(3, diffs.Count);
            // Health changed, Hunger removed, Energy added
            Assert.Contains(diffs, d => d.Key == "Health" && d.OldValue == "100" && d.NewValue == "75");
            Assert.Contains(diffs, d => d.Key == "Hunger" && d.OldValue == "50" && d.NewValue == string.Empty);
            Assert.Contains(diffs, d => d.Key == "Energy" && d.OldValue == string.Empty && d.NewValue == "90");
        }
    }
}
