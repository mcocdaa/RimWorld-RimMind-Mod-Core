using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_FewShotQualityTests
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
        public void Examples_Contain_Multiple_Scenarios()
        {
            var content = ReadSource("DecisionExampleData.cs");

            var actionMatches = Regex.Matches(content, @"\\?""action\\?""\s*:\s*\\?""([a-zA-Z_]+\.[a-zA-Z_]+)");
            var distinctPrefixes = actionMatches.Cast<Match>()
                .Select(m => m.Groups[1].Value.Split('.')[0])
                .Distinct()
                .ToList();

            var distinctActions = actionMatches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            Assert.True(distinctActions.Count >= 3,
                $"DecisionExampleData must contain at least 3 different action patterns, found {distinctActions.Count}: " +
                string.Join(", ", distinctActions));
        }

        [Fact]
        public void Example_Actions_Follow_Mechanism_Action_Format()
        {
            var content = ReadSource("DecisionExampleData.cs");

            bool hasDotSeparatedFormat = Regex.IsMatch(content, @"\\?""action\\?""\s*:\s*\\?""[a-zA-Z_]+\.[a-zA-Z_]+");

            Assert.True(hasDotSeparatedFormat,
                "DecisionExampleData actions must follow mechanism.action dot-separated format");
        }

        [Fact]
        public void Example_Reasons_Are_Non_Trivial()
        {
            var content = ReadSource("DecisionExampleData.cs");

            var reasonMatches = Regex.Matches(content, @"\\?""reason\\?""\s*:\s*\\?""([^""]*?)\\?""");
            Assert.True(reasonMatches.Count > 0, "DecisionExampleData must contain reason fields");

            foreach (Match match in reasonMatches)
            {
                var reasonValue = match.Groups[1].Value;
                Assert.False(string.IsNullOrWhiteSpace(reasonValue),
                    $"Reason value must not be empty or whitespace, found: '{reasonValue}'");
            }
        }

        [Fact]
        public void Examples_Include_ToolCall_Interaction_Pattern()
        {
            var content = ReadSource("DecisionExampleData.cs");

            bool hasToolCallPattern = content.Contains("tool_result")
                || content.Contains("ToolCall")
                || content.Contains("tool_call");

            Assert.True(hasToolCallPattern,
                "DecisionExampleData must include tool_result/ToolCall/tool_call interaction pattern");
        }
    }
}
