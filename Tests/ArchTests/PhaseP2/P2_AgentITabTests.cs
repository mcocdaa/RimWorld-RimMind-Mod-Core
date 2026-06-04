using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP2
{
    public class P2_AgentITabTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        [Fact]
        public void ITab_Pawn_Agent_Exists_In_Infrastructure_Verse()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.NotNull(content);
        }

        [Fact]
        public void ITab_Pawn_Agent_Inherits_ITab_Pawn()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.Contains("ITab_Pawn", content);
        }

        [Fact]
        public void ITab_Pawn_Agent_Shows_WorkflowPhase()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.Contains("WorkflowPhase", content);
        }

        [Fact]
        public void ITab_Pawn_Agent_Shows_Goals()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.Contains("GoalStack", content);
        }

        [Fact]
        public void ITab_Pawn_Agent_Shows_StrategyWeights()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.Contains("StrategyOptimizer", content);
        }

        [Fact]
        public void ITab_Pawn_Agent_Shows_BehaviorHistory()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.Contains("BehaviorHistory", content);
        }

        [Fact]
        public void ITab_Pawn_Agent_Shows_AutonomyLevel()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.Contains("AutonomyLevel", content);
        }

        [Fact]
        public void ITab_Pawn_Agent_Has_CreateAgent_Button_When_No_Agent()
        {
            var content = ReadSourceFile("Infrastructure/Verse/ITab_Pawn_Agent.cs");
            Assert.Contains("RimMind.Agent.ITab.NoAgentHint", content);
            Assert.Contains("RimMind.Agent.ITab.CreateAgent", content);
            Assert.Contains("IPawnAgentFactory", content);
            Assert.Contains("IAgentBus", content);
            Assert.Contains("RimMind.Agent.ITab.CreateFailed", content);
        }
    }
}
