using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP3
{
    public class P3_SchemaConsistencyTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentModes = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Agent", "Modes");

        private static readonly string JsonDir = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Json");

        [Fact]
        public void ProactiveThinkStrategy_SchemaTag_MatchesParserTagName()
        {
            var proactiveCode = File.ReadAllText(Path.Combine(AgentModes, "ProactiveAgentMode.cs"));
            var helperCode = File.ReadAllText(Path.Combine(AgentModes, "ThinkStrategyHelper.cs"));

            Assert.Contains("WithSchema", proactiveCode);
            Assert.Contains("<Action>", proactiveCode);
            Assert.Contains("Action", helperCode);
            Assert.Matches(@"Extract.*ActionJson.*""Action""", helperCode);
        }

        [Fact]
        public void ReactiveThinkStrategy_SchemaTag_MatchesParserTagName()
        {
            var reactiveCode = File.ReadAllText(Path.Combine(AgentModes, "ReactiveAgentMode.cs"));
            var helperCode = File.ReadAllText(Path.Combine(AgentModes, "ThinkStrategyHelper.cs"));

            Assert.Contains("WithSchema", reactiveCode);
            Assert.Contains("<Action>", reactiveCode);
            Assert.Matches(@"Extract.*ActionJson.*""Action""", helperCode);
        }

        [Fact]
        public void BothStrategies_UseSameSchemaTag()
        {
            var proactiveCode = File.ReadAllText(Path.Combine(AgentModes, "ProactiveAgentMode.cs"));
            var reactiveCode = File.ReadAllText(Path.Combine(AgentModes, "ReactiveAgentMode.cs"));

            var proactiveSchemaLines = proactiveCode.Split('\n')
                .Where(l => l.Contains("WithSchema")).ToList();
            var reactiveSchemaLines = reactiveCode.Split('\n')
                .Where(l => l.Contains("WithSchema")).ToList();

            Assert.Single(proactiveSchemaLines);
            Assert.Single(reactiveSchemaLines);
            Assert.Equal(proactiveSchemaLines[0].Trim(), reactiveSchemaLines[0].Trim());
        }

        [Fact]
        public void JsonTagExtractor_ExistsAndHasExtractMethod()
        {
            var code = File.ReadAllText(Path.Combine(JsonDir, "JsonTagExtractor.cs"));
            Assert.Contains("public static T? Extract<T>", code);
            Assert.Contains("tagName", code);
        }
    }
}
