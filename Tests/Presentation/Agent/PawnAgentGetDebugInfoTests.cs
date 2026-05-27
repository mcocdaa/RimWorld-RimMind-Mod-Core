using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.Presentation.Agent
{
    /// <summary>
    /// Verifies that PawnAgent.GetDebugInfo includes CurrentModeId,
    /// PerceptionBuffer state, and LastThinkTick for better runtime debugging.
    ///
    /// Uses source-file reading because PawnAgent.cs depends on Verse types
    /// not available in the net10.0 test project.
    /// </summary>
    public class PawnAgentGetDebugInfoTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string PawnAgentPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "PawnAgent.cs");

        private static string ReadSource()
        {
            Assert.True(File.Exists(PawnAgentPath), $"PawnAgent.cs must exist at {PawnAgentPath}");
            return File.ReadAllText(PawnAgentPath);
        }

        [Fact]
        public void GetDebugInfo_Contains_CurrentModeId()
        {
            var source = ReadSource();

            // GetDebugInfo should output CurrentModeId
            Assert.Contains("CurrentModeId:", source);
        }

        [Fact]
        public void GetDebugInfo_Contains_PerceptionBuffer()
        {
            var source = ReadSource();

            // GetDebugInfo should output PerceptionBuffer entry count
            Assert.Contains("PerceptionBuffer:", source);
        }

        [Fact]
        public void GetDebugInfo_Contains_LastThinkTick()
        {
            var source = ReadSource();

            // GetDebugInfo should output LastThinkTick
            Assert.Contains("LastThinkTick:", source);
        }
    }
}
