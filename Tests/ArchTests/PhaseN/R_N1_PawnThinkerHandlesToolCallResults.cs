using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    /// <summary>
    /// R_N1: PawnThinker handles ToolCallResults, proving the Agentic Loop is closed.
    /// The think callback must read ToolCallResults from the pipeline context
    /// and pass them into ParseDecision for multi-round tool call orchestration.
    /// </summary>
    public class R_N1_PawnThinkerHandlesToolCallResults
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string PawnThinkerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "PawnThinker.cs");

        [Fact]
        public void PawnThinker_References_ToolCallResults()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("ToolCallResults", content);
        }

        [Fact]
        public void PawnThinker_Passes_ToolCallResults_To_ParseDecision()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("ParseDecision", content);
            Assert.Contains("toolCallResults", content);
        }

        [Fact]
        public void PawnThinker_Has_Agentic_Loop_Recursion()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("WantsMoreToolCalls", content);
            Assert.Contains("SendThinkRequest", content);
        }
    }
}
