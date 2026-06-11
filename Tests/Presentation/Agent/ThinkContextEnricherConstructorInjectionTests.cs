using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.Presentation.Agent
{
    public class ThinkContextEnricherConstructorInjectionTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string EnricherPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "ThinkContextEnricher.cs");

        private static string ReadSource()
        {
            Assert.True(File.Exists(EnricherPath), $"ThinkContextEnricher.cs must exist at {EnricherPath}");
            return File.ReadAllText(EnricherPath);
        }

        [Fact]
        public void ThinkContextEnricher_DoesNotUse_RimMindServiceLocator()
        {
            var source = ReadSource();
            Assert.DoesNotContain("RimMindServiceLocator", source);
        }

        [Fact]
        public void ThinkContextEnricher_DoesNotImport_ServiceLocatorNamespace()
        {
            var source = ReadSource();
            Assert.DoesNotContain("using RimMind.Application.Common.Interfaces.Internal;", source);
        }

        [Fact]
        public void ThinkContextEnricher_HasConstructor_WithVoiceHandlerParam()
        {
            var source = ReadSource();
            Assert.Contains("InnerVoiceHandler? voiceHandler", source);
        }

        [Fact]
        public void ThinkContextEnricher_HasConstructor_WithPsychologyWatcherParam()
        {
            var source = ReadSource();
            Assert.Contains("IPsychologyWatcher? psychologyWatcher", source);
        }

        [Fact]
        public void ThinkContextEnricher_ConstructorParams_AreOptional_NullDefault()
        {
            var source = ReadSource();
            Assert.Contains("voiceHandler = null", source);
            Assert.Contains("psychologyWatcher = null", source);
        }

        [Fact]
        public void ThinkContextEnricher_Fields_AreReadOnly()
        {
            var source = ReadSource();
            var lines = source.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.StartsWith("private readonly") && (l.Contains("_innerVoiceHandler") || l.Contains("_psychologyWatcher")))
                .ToList();

            Assert.Equal(2, lines.Count);
            Assert.All(lines, l => Assert.StartsWith("private readonly", l));
        }

        [Fact]
        public void ThinkContextEnricher_AssignsInjectedFields_InConstructor()
        {
            var source = ReadSource();
            Assert.Contains("_innerVoiceHandler = voiceHandler", source);
            Assert.Contains("_psychologyWatcher = psychologyWatcher", source);
        }

        [Fact]
        public void ThinkContextEnricher_NoLazyServiceLocatorResolution()
        {
            var source = ReadSource();
            Assert.DoesNotContain("_innerVoiceHandler ??= RimMindServiceLocator", source);
            Assert.DoesNotContain("_psychologyWatcher ??= RimMindServiceLocator", source);
            Assert.DoesNotContain("GetInnerVoiceHandler()", source);
            Assert.DoesNotContain("GetPsychologyWatcher()", source);
        }

        [Fact]
        public void ThinkContextEnricher_ConsumeInnerVoice_UsesFieldDirectly()
        {
            var source = ReadSource();
            Assert.Contains("_innerVoiceHandler?.GetPendingVoiceText", source);
            Assert.Contains("_innerVoiceHandler?.ClearVoice", source);
        }

        [Fact]
        public void ThinkContextEnricher_CheckPsychology_UsesFieldDirectly()
        {
            var source = ReadSource();
            Assert.Contains("_psychologyWatcher?.CheckAndPublish", source);
        }

        [Fact]
        public void ThinkContextEnricher_EnrichEnvelope_UsesFieldDirectly()
        {
            var source = ReadSource();
            Assert.Contains("_psychologyWatcher?.HasUrgentEvent", source);
        }

        [Fact]
        public void PawnThinker_PassesServiceLocatorResults_ToThinkContextEnricher()
        {
            var thinkerPath = Path.Combine(
                RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "PawnThinker.cs");
            Assert.True(File.Exists(thinkerPath), $"PawnThinker.cs must exist at {thinkerPath}");

            var source = File.ReadAllText(thinkerPath);
            Assert.Contains("RimMindServiceLocator.TryGet<InnerVoiceHandler>()", source);
            Assert.Contains("RimMindServiceLocator.TryGet<IPsychologyWatcher>()", source);
            Assert.Contains("new ThinkContextEnricher(", source);
        }
    }
}
