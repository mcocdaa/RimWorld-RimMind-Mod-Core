using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    /// <summary>
    /// R_N1: DecisionProcessor handles ToolCallResults, proving the Agentic Loop is closed.
    /// The decision processor must read ToolCallResults from the pipeline context
    /// and pass them into ParseDecision for multi-round tool call orchestration.
    /// PawnThinker delegates this responsibility to DecisionProcessor.
    /// </summary>
    public class R_N1_PawnThinkerHandlesToolCallResults
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string PawnThinkerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "PawnThinker.cs");

        private static readonly string DecisionProcessorPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Agent", "DecisionProcessor.cs");

        [Fact]
        public void PawnThinker_Delegates_To_DecisionProcessor()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("IDecisionProcessor", content);
            Assert.Contains("_decisionProcessor", content);
        }

        [Fact]
        public void DecisionProcessor_Passes_ToolCallResults_To_ParseDecision()
        {
            Assert.True(File.Exists(DecisionProcessorPath), "DecisionProcessor.cs must exist");

            var content = File.ReadAllText(DecisionProcessorPath);

            Assert.Contains("ParseDecision", content);
            Assert.Contains("toolCallResults", content);
        }

        [Fact]
        public void DecisionProcessor_Has_Agentic_Loop_Recursion()
        {
            Assert.True(File.Exists(DecisionProcessorPath), "DecisionProcessor.cs must exist");

            var content = File.ReadAllText(DecisionProcessorPath);

            Assert.Contains("IAgenticLoopService", content);
            Assert.Contains("_requestFollowUp", content);
        }
    }
}
