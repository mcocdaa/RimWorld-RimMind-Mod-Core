using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_FewShotQualityTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static string GetThinkStrategyHelperSource()
        {
            var file = Directory.GetFiles(ProjectRoot, "ThinkStrategyHelper.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Agent") && f.Contains("Modes"))
                ?? throw new FileNotFoundException("ThinkStrategyHelper.cs not found");
            return File.ReadAllText(file);
        }

        private static string GetDecisionExampleDataSource()
        {
            var file = Directory.GetFiles(ProjectRoot, "DecisionExampleData.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Agent") && f.Contains("Modes"))
                ?? throw new FileNotFoundException("DecisionExampleData.cs not found");
            return File.ReadAllText(file);
        }

        [Fact]
        public void BuildDecisionExamples_Method_Exists()
        {
            var content = GetThinkStrategyHelperSource();
            Assert.Contains("BuildDecisionExamples", content);
        }

        [Fact]
        public void FewShotExamples_ContainActionTag()
        {
            var content = GetDecisionExampleDataSource();
            var actionTagCount = Regex.Matches(content, @"<Action>").Count;
            Assert.True(actionTagCount >= 3, $"Expected at least 3 <Action> tags in few-shot examples, found {actionTagCount}");
        }

        [Fact]
        public void FewShotExamples_ActionFormat_ContainsDot()
        {
            var content = GetDecisionExampleDataSource();
            Assert.Contains("pawn.job.force_rest", content);
            Assert.Contains("pawn.draft.toggle", content);
            Assert.Contains("pawn.work.set", content);
        }

        [Fact]
        public void FewShotExamples_ContainReasonField()
        {
            var content = GetDecisionExampleDataSource();
            var reasonMatches = Regex.Matches(content, @"reason");
            Assert.True(reasonMatches.Count >= 3,
                $"Expected at least 3 reason fields in few-shot examples, found {reasonMatches.Count}");
        }
    }
}
