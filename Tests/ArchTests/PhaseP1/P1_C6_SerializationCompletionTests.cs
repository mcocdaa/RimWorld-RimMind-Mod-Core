using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-C6: Serialization Completion.
    /// Verifies that PawnAgent.ExposeData includes _lastThinkTick serialization,
    /// and that PawnThinker has RestoreLastThinkTick for post-load restoration.
    /// </summary>
    public class P1_C6_SerializationCompletionTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentDir = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Agent");

        private static readonly string PawnAgentPath = Path.Combine(AgentDir, "PawnAgent.cs");
        private static readonly string PawnThinkerPath = Path.Combine(AgentDir, "PawnThinker.cs");

        [Fact]
        public void PawnAgent_ExposeData_Contains_LastThinkTick()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("lastThinkTick", source);
        }

        [Fact]
        public void PawnAgent_Has_LastThinkTick_Field()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("private int _lastThinkTick;", source);
        }

        [Fact]
        public void PawnAgent_ExposeData_Serializes_LastThinkTick_With_Scribe()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("Scribe_Values.Look(ref _lastThinkTick", source);
        }

        [Fact]
        public void PawnThinker_Has_RestoreLastThinkTick_Method()
        {
            var source = File.ReadAllText(PawnThinkerPath);
            Assert.Contains("RestoreLastThinkTick", source);
        }

        [Fact]
        public void PawnAgent_RebuildCollaborators_Restores_LastThinkTick()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("RestoreLastThinkTick", source);
        }

        [Fact]
        public void PawnThinker_RestoreLastThinkTick_Is_Internal()
        {
            var source = File.ReadAllText(PawnThinkerPath);
            Assert.Contains("internal void RestoreLastThinkTick", source);
        }
    }
}
