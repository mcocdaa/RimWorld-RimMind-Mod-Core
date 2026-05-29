using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    public class R_N4_ProactiveExecutorNoSyncBlocking
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string ProactiveExecutorPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "ProactiveBehaviorExecutor.cs");

        private static readonly string OrchestratorPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Agent", "ProactiveBehaviorOrchestrator.cs");

        private static readonly Regex SyncBlockingPattern =
            new Regex(@"\)\s*\.Result\s*;", RegexOptions.Compiled);

        [Fact]
        public void ProactiveBehaviorExecutor_No_Sync_Blocking_Result_Calls()
        {
            Assert.True(File.Exists(ProactiveExecutorPath), "ProactiveBehaviorExecutor.cs must exist");
            var content = File.ReadAllText(ProactiveExecutorPath);
            Assert.Empty(SyncBlockingPattern.Matches(content));
        }

        [Fact]
        public void ProactiveBehaviorOrchestrator_No_Sync_Blocking_Result_Calls()
        {
            Assert.True(File.Exists(OrchestratorPath), "ProactiveBehaviorOrchestrator.cs must exist");
            var content = File.ReadAllText(OrchestratorPath);
            Assert.Empty(SyncBlockingPattern.Matches(content));
        }

        [Fact]
        public void ProactiveBehaviorOrchestrator_Uses_ContinueWith_Pattern()
        {
            Assert.True(File.Exists(OrchestratorPath), "ProactiveBehaviorOrchestrator.cs must exist");
            var content = File.ReadAllText(OrchestratorPath);
            Assert.Contains("ContinueWith", content);
            Assert.Contains("TaskScheduler.Current", content);
        }
    }
}
