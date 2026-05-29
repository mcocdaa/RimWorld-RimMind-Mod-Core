using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP3
{
    public class P3_PerceptionDedupTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentModes = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Agent", "Modes");

        private static readonly string Enricher = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Agent", "ThinkContextEnricher.cs");

        [Fact]
        public void ThinkStrategyHelper_FormatPerceptions_UsesXmlTag()
        {
            var code = File.ReadAllText(Path.Combine(AgentModes, "ThinkStrategyHelper.cs"));
            Assert.Contains("<perceptions>", code);
            Assert.Contains("</perceptions>", code);
        }

        [Fact]
        public void ThinkContextEnricher_EnrichEnvelope_UsesAddSection()
        {
            var code = File.ReadAllText(Enricher);
            Assert.Contains("AddSection(\"inner_voice\"", code);
            Assert.Contains("AddSection(\"psychology_alert\"", code);
        }

        [Fact]
        public void ThinkContextEnricher_FormatBehaviorHistory_UsesXmlTag()
        {
            var code = File.ReadAllText(Enricher);
            Assert.Contains("<behavior_history>", code);
            Assert.Contains("</behavior_history>", code);
        }

        [Fact]
        public void ThinkContextEnricher_FormatToolCallResults_UsesXmlTag()
        {
            var code = File.ReadAllText(Enricher);
            Assert.Contains("<tool_call_results", code);
            Assert.Contains("</tool_call_results>", code);
        }
    }
}
