using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP3
{
    public class P3_FewShotExampleTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string DomainLlm = Path.Combine(
            ProjectRoot, "Source", "Domain", "Llm");

        private static readonly string ApplicationLlm = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Llm");

        private static readonly string AgentModes = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Agent", "Modes");

        [Fact]
        public void LlmRequestEnvelope_HasExamplesProperty()
        {
            var code = File.ReadAllText(Path.Combine(DomainLlm, "LlmRequestEnvelope.cs"));
            Assert.Contains("Examples", code);
        }

        [Fact]
        public void LlmRequestEnvelopeBuilder_HasWithExamplesMethod()
        {
            var code = File.ReadAllText(Path.Combine(ApplicationLlm, "LlmRequestEnvelopeBuilder.cs"));
            Assert.Contains("WithExamples", code);
        }

        [Fact]
        public void ProactiveThinkStrategy_BuildEnvelope_UsesWithExamples()
        {
            var code = File.ReadAllText(Path.Combine(AgentModes, "ProactiveAgentMode.cs"));
            Assert.Contains("WithExamples", code);
        }

        [Fact]
        public void ReactiveThinkStrategy_BuildEnvelope_UsesWithExamples()
        {
            var code = File.ReadAllText(Path.Combine(AgentModes, "ReactiveAgentMode.cs"));
            Assert.Contains("WithExamples", code);
        }

        [Fact]
        public void ProactiveThinkStrategy_ExampleContainsActionTag()
        {
            var code = File.ReadAllText(Path.Combine(AgentModes, "ProactiveAgentMode.cs"));
            Assert.Matches(@"<Action>.*</Action>", code);
        }

        [Fact]
        public void ReactiveThinkStrategy_ExampleContainsActionTag()
        {
            var code = File.ReadAllText(Path.Combine(AgentModes, "ReactiveAgentMode.cs"));
            Assert.Matches(@"<Action>.*</Action>", code);
        }
    }
}
