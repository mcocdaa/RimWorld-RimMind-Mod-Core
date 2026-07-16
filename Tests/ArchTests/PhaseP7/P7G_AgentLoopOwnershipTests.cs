using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP7
{
    public class P7G_AgentLoopOwnershipTests
    {
        private const string SchedulerHostCall = "_scheduler?.Tick(now)";

        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static IReadOnlyList<(string Path, string Content)> ReadProductionSources()
            => Directory.GetFiles(SourceDir, "*.cs", SearchOption.AllDirectories)
                .Select(path => (Path.GetRelativePath(SourceDir, path), File.ReadAllText(path)))
                .ToList();

        [Fact]
        public void RuntimeComponent_IsThinSchedulerHostWithoutLegacyPawnRegistryOrSaveApi()
        {
            string content = ReadSource("Presentation/Runtime/RimMindRuntimeGameComponent.cs");

            Assert.Contains("IAgentLoopScheduler", content);
            Assert.Contains(SchedulerHostCall, content);

            string[] forbidden =
            {
                "Dictionary<int, IPawnAgentVerse>",
                "_agents",
                "Scribe_Collections.Look",
                "LookMode.Deep",
                "GetOrCreateAgent(",
                "GetAgent(int pawnId)",
                "RemoveAgent(int pawnId)",
                "[Obsolete",
                "ExposeData(",
            };

            foreach (var token in forbidden)
                Assert.DoesNotContain(token, content);
        }

        [Fact]
        public void CompPawnAgent_RegistersWithSchedulerInsteadOfTickingAgentDirectly()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            string compTick = ExtractMethodBody(content, "public override void CompTick()");

            Assert.Contains("EnsureAgentLoopRegistration();", compTick);
            Assert.DoesNotContain("Agent?.Tick()", compTick);
            Assert.DoesNotContain("Agent.Tick()", compTick);
        }

        [Fact]
        public void Production_HasExactlyOneSchedulerHostCallInRuntimeComponent()
        {
            var matches = ReadProductionSources()
                .Where(source => source.Content.Contains(SchedulerHostCall, StringComparison.Ordinal))
                .ToList();

            Assert.Single(matches);
            Assert.Equal(
                Path.Combine("Presentation", "Runtime", "RimMindRuntimeGameComponent.cs"),
                matches[0].Path);
            Assert.Equal(1, CountOccurrences(matches[0].Content, SchedulerHostCall));
        }

        [Fact]
        public void RuntimeLifecycle_ClearsScopedAgentsBeforeSchedulerForNewAndLoadedGames()
        {
            string content = ReadSource("Presentation/Runtime/RimMindRuntimeGameComponent.cs");

            Assert.Contains("StartedNewGame", content);
            Assert.Contains("LoadedGame", content);
            Assert.Contains("_scopedAgentManager?.Clear()", content);
            Assert.Contains("_scheduler?.Clear()", content);
            Assert.Contains(
                "ResetRuntimeAgents();",
                ExtractMethodBody(content, "public override void StartedNewGame()"));
            Assert.Contains(
                "ResetRuntimeAgents();",
                ExtractMethodBody(content, "public override void LoadedGame()"));
            Assert.True(
                content.IndexOf("_scopedAgentManager?.Clear()", StringComparison.Ordinal)
                < content.IndexOf("_scheduler?.Clear()", StringComparison.Ordinal),
                "Scoped agents must be cleared before the scheduler registry is invalidated.");
        }

        [Fact]
        public void SchedulerGeneration_IsLongAndLockFree()
        {
            string content = ReadSource("Application/Features/Agent/AgentLoopScheduler.cs");

            Assert.Contains("private long _generation;", content);
            Assert.Contains("public long Generation => Interlocked.Read(ref _generation);", content);
        }

        [Fact]
        public void RuntimeBehaviorTests_CompileTheProductionGameComponent()
        {
            string project = File.ReadAllText(Path.Combine(ProjectRoot, "Tests", "RimMindCore.Tests.csproj"));

            Assert.Contains("Presentation\\Runtime\\*.cs", project);
            Assert.Contains("../Source/Presentation/Runtime/RimMindRuntimeGameComponent.cs", project);
        }

        [Fact]
        public void AgentLoopScheduler_IsConstructedOnceInProductionCompositionRoot()
        {
            const string construction = "new AgentLoopScheduler(";
            var matches = ReadProductionSources()
                .Where(source => source.Content.Contains(construction, StringComparison.Ordinal))
                .ToList();

            Assert.Single(matches);
            Assert.Equal(
                Path.Combine("Presentation", "Runtime", "Composition", "AgentComposition.cs"),
                matches[0].Path);
            Assert.Equal(1, CountOccurrences(matches[0].Content, construction));
        }

        private static int CountOccurrences(string content, string token)
        {
            var count = 0;
            var start = 0;
            while ((start = content.IndexOf(token, start, StringComparison.Ordinal)) >= 0)
            {
                count++;
                start += token.Length;
            }

            return count;
        }

        private static string ExtractMethodBody(string source, string methodSignature)
        {
            int methodStart = source.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.True(methodStart >= 0, $"Method signature '{methodSignature}' not found.");
            int braceStart = source.IndexOf('{', methodStart);
            Assert.True(braceStart >= 0, "Opening brace not found after method signature.");

            int depth = 1;
            int pos = braceStart + 1;
            while (pos < source.Length && depth > 0)
            {
                if (source[pos] == '{') depth++;
                else if (source[pos] == '}') depth--;
                pos++;
            }

            return source.Substring(braceStart + 1, pos - braceStart - 2);
        }
    }
}
