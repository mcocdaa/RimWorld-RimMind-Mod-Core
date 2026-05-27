using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    /// <summary>
    /// R_N4: ProactiveBehaviorExecutor does NOT contain synchronous .Result blocking calls,
    /// proving async migration is complete. ContinueWith callbacks using t.Result are allowed;
    /// only direct Async().Result blocking patterns are forbidden.
    /// </summary>
    public class R_N4_ProactiveExecutorNoSyncBlocking
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string ProactiveExecutorPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "ProactiveBehaviorExecutor.cs");

        /// <summary>
        /// Detects synchronous blocking pattern: SomeAsync(...).Result
        /// This is the pattern that deadlocks in RimWorld's main thread.
        /// ContinueWith callbacks with t.Result are NOT blocked — they run after completion.
        /// </summary>
        private static readonly Regex SyncBlockingPattern =
            new Regex(@"\)\s*\.Result\s*;", RegexOptions.Compiled);

        [Fact]
        public void ProactiveBehaviorExecutor_No_Sync_Blocking_Result_Calls()
        {
            Assert.True(File.Exists(ProactiveExecutorPath), "ProactiveBehaviorExecutor.cs must exist");

            var content = File.ReadAllText(ProactiveExecutorPath);

            var matches = SyncBlockingPattern.Matches(content);
            Assert.Empty(matches);
        }

        [Fact]
        public void ProactiveBehaviorExecutor_Uses_ContinueWith_Pattern()
        {
            Assert.True(File.Exists(ProactiveExecutorPath), "ProactiveBehaviorExecutor.cs must exist");

            var content = File.ReadAllText(ProactiveExecutorPath);

            Assert.Contains("ContinueWith", content);
            Assert.Contains("TaskScheduler.Current", content);
        }
    }
}
