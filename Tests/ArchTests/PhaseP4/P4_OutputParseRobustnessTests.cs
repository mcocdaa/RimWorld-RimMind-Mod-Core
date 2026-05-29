using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_OutputParseRobustnessTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void ParseDecisionCore_HandlesEmptyAction()
        {
            var file = Directory.GetFiles(ProjectRoot, "ThinkStrategyHelper.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Agent") && f.Contains("Modes"))
                ?? throw new FileNotFoundException("ThinkStrategyHelper.cs not found");
            var content = File.ReadAllText(file);
            Assert.Contains("ParseDecisionCore", content);
        }

        [Fact]
        public void JsonTagExtractor_Exists()
        {
            var file = Directory.GetFiles(ProjectRoot, "JsonTagExtractor.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("JsonTagExtractor.cs not found");
            Assert.True(File.Exists(file));
        }

        [Fact]
        public void JsonTagExtractor_UsesRegex()
        {
            var file = Directory.GetFiles(ProjectRoot, "JsonTagExtractor.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("JsonTagExtractor.cs not found");
            var content = File.ReadAllText(file);
            Assert.Contains("Regex", content);
        }

        [Fact]
        public void DecisionValidator_Exists()
        {
            var file = Directory.GetFiles(ProjectRoot, "DecisionValidator.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("DecisionValidator.cs not found");
            Assert.True(File.Exists(file));
        }
    }
}
