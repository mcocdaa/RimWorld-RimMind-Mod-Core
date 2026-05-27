using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    /// <summary>
    /// R_N2: PawnActor.ExecuteDecision is implemented (not a shell).
    /// The method must delegate to IActionExecutor instead of being empty
    /// or returning a hardcoded value.
    /// </summary>
    public class R_N2_PawnActorExecuteDecisionImplemented
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string PawnActorPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "PawnActor.cs");

        [Fact]
        public void PawnActor_ExecuteDecision_Delegates_To_IActionExecutor()
        {
            Assert.True(File.Exists(PawnActorPath), "PawnActor.cs must exist");

            var content = File.ReadAllText(PawnActorPath);

            Assert.Contains("ExecuteDecision", content);
            Assert.Contains("_actionExecutor.ExecuteDecision", content);
        }

        [Fact]
        public void PawnActor_Has_IActionExecutor_Field()
        {
            Assert.True(File.Exists(PawnActorPath), "PawnActor.cs must exist");

            var content = File.ReadAllText(PawnActorPath);

            Assert.Contains("IActionExecutor", content);
            Assert.Contains("_actionExecutor", content);
        }

        [Fact]
        public void PawnActor_ExecuteDecision_Is_Not_Empty_Shell()
        {
            Assert.True(File.Exists(PawnActorPath), "PawnActor.cs must exist");

            var content = File.ReadAllText(PawnActorPath);

            // Must contain the method signature and delegation logic
            Assert.Contains("public Result<Unit, RimMindError> ExecuteDecision(AgentDecision decision)", content);
            Assert.Contains("_actionExecutor.ExecuteDecision(decision, _agent.Pawn.thingIDNumber)", content);
        }
    }
}
