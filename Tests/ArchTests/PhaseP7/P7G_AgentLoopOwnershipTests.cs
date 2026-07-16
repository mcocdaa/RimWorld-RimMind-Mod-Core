using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public void AgentBusGameComponent_IsTheOnlyVerseQueueTickCoordinatorHost()
        {
            const string flushMember = "FlushBackgroundQueue";
            const string coordinatorTick = "_tickCoordinator?.Tick(Find.TickManager.TicksGame)";

            string agentBusComponent = ReadSource("Infrastructure/Verse/AgentBusGameComponent.cs");
            Assert.Contains("AgentBusQueueTickCoordinator", agentBusComponent);
            Assert.Equal(1, CountOccurrences(agentBusComponent, coordinatorTick));

            string queueComponent = ReadSource("Infrastructure/Verse/AIRequestQueueGameComponent.cs");
            Assert.DoesNotContain("GameComponentTick", queueComponent);
            Assert.DoesNotContain(".Tick()", queueComponent);
            Assert.DoesNotContain(flushMember, queueComponent);

            string coordinator = ReadSource("Application/Features/Queue/AgentBusQueueTickCoordinator.cs");
            int flushIndex = coordinator.IndexOf("_agentBus.FlushBackgroundQueue()", StringComparison.Ordinal);
            int queueTickIndex = coordinator.IndexOf("_queue.Tick()", StringComparison.Ordinal);
            Assert.True(flushIndex >= 0, "Coordinator must flush the AgentBus.");
            Assert.True(queueTickIndex > flushIndex, "Coordinator must tick the queue after the AgentBus flush.");

            string[] queueTypes =
            {
                "Application/Common/Interfaces/Internal/IAIRequestQueueTickable.cs",
                "Application/Features/Queue/AIRequestQueueImpl.cs",
            };

            foreach (string path in queueTypes)
            {
                string content = ReadSource(path);
                Assert.DoesNotContain(flushMember, content);
                Assert.DoesNotContain("[Obsolete", content);
            }

            var coordinatorHosts = ReadProductionSources()
                .Where(source => source.Content.Contains(coordinatorTick, StringComparison.Ordinal))
                .ToList();

            Assert.Single(coordinatorHosts);
            Assert.Equal(
                Path.Combine("Infrastructure", "Verse", "AgentBusGameComponent.cs"),
                coordinatorHosts[0].Path);

            var directFlushOwners = ReadProductionSources()
                .Where(source => Regex.IsMatch(source.Content, @"\.\s*FlushBackgroundQueue\s*\("))
                .ToList();

            Assert.Single(directFlushOwners);
            Assert.Equal(
                Path.Combine("Application", "Features", "Queue", "AgentBusQueueTickCoordinator.cs"),
                directFlushOwners[0].Path);

            var directQueueTickOwners = ReadProductionSources()
                .Where(source => Regex.IsMatch(
                    source.Content,
                    @"\b(?:_?queue|_?requestQueue|_?impl)\s*\??\.\s*Tick\s*\("))
                .ToList();

            Assert.Single(directQueueTickOwners);
            Assert.Equal(
                Path.Combine("Application", "Features", "Queue", "AgentBusQueueTickCoordinator.cs"),
                directQueueTickOwners[0].Path);
        }

        [Fact]
        public void AgentBusGameComponent_ReconcilesCoreSubscribersAfterLifecycleAndBusReplacement()
        {
            string content = ReadSource("Infrastructure/Verse/AgentBusGameComponent.cs");
            string ensureCached = ExtractMethodBody(content, "private void EnsureCached()");

            Assert.Contains("private IAgentBus? _subscribersRegisteredBus;", content);
            Assert.Contains("private bool _lifecycleStarted;", content);
            Assert.Contains("_lifecycleStarted", ensureCached);
            Assert.Contains("!ReferenceEquals(_subscribersRegisteredBus, _agentBus)", ensureCached);
            Assert.Contains("ReRegisterCoreSubscribers();", ensureCached);
            Assert.Contains(
                "_lifecycleStarted = true;",
                ExtractMethodBody(content, "public override void StartedNewGame()"));
            Assert.Contains(
                "_lifecycleStarted = true;",
                ExtractMethodBody(content, "public override void LoadedGame()"));
        }

        [Fact]
        public void DebugCenterOverview_UsesSchedulerSnapshotAndBothDiagnosticLocales()
        {
            string drawer = ReadSource(
                "Infrastructure/UI/DebugCenter/Pages/OverviewDebugCenterPageDrawer.cs");

            Assert.Contains("IAgentLoopScheduler", drawer);
            Assert.Contains("GetSnapshot()", drawer);
            Assert.Contains("AgentLoopSnapshot.Empty", drawer);
            Assert.DoesNotContain("AllPawnsSpawned", drawer);
            Assert.DoesNotContain("mapPawns", drawer);
            Assert.Contains(
                "\"RimMind.UI.Hub.AgentLoop\".Translate(), BuildAgentLoopSummary(model)",
                drawer);
            Assert.Contains(
                "\"RimMind.UI.Hub.AgentLoopLastTick\".Translate(), BuildLastAgentLoopTick(model)",
                drawer);
            Assert.Contains(
                "\"RimMind.UI.Hub.AgentLoopFaults\".Translate(), model.AgentLoopFaults.ToString()",
                drawer);
            Assert.Contains("loop.ActiveAgents,", drawer);
            Assert.Contains("loop.PausedAgents,", drawer);
            Assert.Contains("loop.PendingAgents,", drawer);
            Assert.Contains("loop.TerminatedAgents,", drawer);
            Assert.Contains("loop.RegisteredPawnAgents,", drawer);
            Assert.Contains("loop.RegisteredScopedAgents,", drawer);
            Assert.Contains("loop.LastTick,", drawer);
            Assert.Contains("loop.FaultedAgents);", drawer);

            string english = File.ReadAllText(Path.Combine(
                ProjectRoot, "Languages", "English", "Keyed", "RimMind_Core.xml"));
            string chinese = File.ReadAllText(Path.Combine(
                ProjectRoot, "Languages", "ChineseSimplified", "Keyed", "RimMind_Core.xml"));

            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoop>Agent runtime loop</RimMind.UI.Hub.AgentLoop>",
                english);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopLastTick>Last loop tick</RimMind.UI.Hub.AgentLoopLastTick>",
                english);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopFaults>Loop faults</RimMind.UI.Hub.AgentLoopFaults>",
                english);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentSummary>Runtime agent states</RimMind.UI.Hub.AgentSummary>",
                english);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopPawn>pawn</RimMind.UI.Hub.AgentLoopPawn>",
                english);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopScoped>scoped</RimMind.UI.Hub.AgentLoopScoped>",
                english);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopNeverRun>Not run</RimMind.UI.Hub.AgentLoopNeverRun>",
                english);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoop>Agent 运行循环</RimMind.UI.Hub.AgentLoop>",
                chinese);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopLastTick>最近循环 Tick</RimMind.UI.Hub.AgentLoopLastTick>",
                chinese);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopFaults>循环故障数</RimMind.UI.Hub.AgentLoopFaults>",
                chinese);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentSummary>运行时 Agent 状态</RimMind.UI.Hub.AgentSummary>",
                chinese);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopPawn>小人</RimMind.UI.Hub.AgentLoopPawn>",
                chinese);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopScoped>作用域</RimMind.UI.Hub.AgentLoopScoped>",
                chinese);
            Assert.Contains(
                "<RimMind.UI.Hub.AgentLoopNeverRun>尚未运行</RimMind.UI.Hub.AgentLoopNeverRun>",
                chinese);
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
