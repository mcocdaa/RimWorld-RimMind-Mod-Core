using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_ContextAssemblyQualityTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static string ReadSource(string fileName)
        {
            var file = Directory.GetFiles(ProjectRoot, fileName, SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains("backup") && !f.Contains("obj"))
                ?? throw new FileNotFoundException($"{fileName} not found");
            return File.ReadAllText(file);
        }

        [Fact]
        public void ContextLayerBuilder_Generates_Xml_Tags()
        {
            var content = ReadSource("ContextLayerBuilder.cs");

            Assert.Contains("BuildLayer", content);
            Assert.Contains("xmlTag", content);
        }

        [Fact]
        public void Context_Orchestrator_Has_Budget_Trim()
        {
            var content = ReadSource("ContextOrchestrator.cs");

            bool hasBudgetTrim = content.Contains("ApplyBudgetTrim") || content.Contains("BudgetTrim");
            Assert.True(hasBudgetTrim,
                "ContextOrchestrator must contain ApplyBudgetTrim or BudgetTrim method");
        }

        [Fact]
        public void Context_Snapshot_Contains_Layer_Meta()
        {
            var content = ReadSource("ContextSnapshot.cs");

            Assert.Contains("Layer", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Meta", content);
        }

        [Fact]
        public void Diff_Tracker_Integration_Exists()
        {
            var content = ReadSource("ContextOrchestrator.cs");

            bool hasDiffTracker = content.Contains("DiffTracker") || content.Contains("BuildDiffMessage");
            Assert.True(hasDiffTracker,
                "ContextOrchestrator must integrate DiffTracker or BuildDiffMessage");
        }
    }
}
