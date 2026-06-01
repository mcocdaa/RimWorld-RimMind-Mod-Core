using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP2
{
    public class P2_PawnVisibilityTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private const string CompPawnAgentRelative = "Infrastructure/Verse/CompPawnAgent.cs";
        private const string WindowAgentStateDebugRelative = "Infrastructure/UI/Window_AgentStateDebug.cs";
        private const string WindowAgentModeDebugRelative = "Infrastructure/UI/Window_AgentModeDebug.cs";
        private const string WindowAgentFlowLabRelative = "Infrastructure/UI/Window_AgentFlowLab.cs";

        [Fact]
        public void CompPawnAgent_Shows_Gizmos_When_Agent_Null()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("RimMind.Agent.Gizmo.CreateAgent", content);
            Assert.Contains("RimMind.Agent.Gizmo.ViewState", content);
        }

        [Fact]
        public void CompPawnAgent_CreateAgent_Gizmo_Uses_RimMind_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            var agentNullBlockStart = content.IndexOf("if (Agent == null)");
            Assert.True(agentNullBlockStart > 0, "Agent == null block must exist");
            var yieldBreakPos = content.IndexOf("yield break", agentNullBlockStart);
            Assert.True(yieldBreakPos > 0, "Agent == null block must end with yield break");
            var blockContent = content.Substring(agentNullBlockStart, yieldBreakPos - agentNullBlockStart);
            Assert.Contains("RimMindIcon", blockContent);
        }

        [Fact]
        public void CompPawnAgent_ViewState_Gizmo_Exists_When_Agent_Present()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            var viewStateCount = 0;
            var idx = 0;
            while ((idx = content.IndexOf("RimMind.Agent.Gizmo.ViewState", idx)) != -1)
            {
                viewStateCount++;
                idx++;
            }
            Assert.True(viewStateCount >= 2,
                "ViewState gizmo must appear at least twice: once in Agent==null branch, once when Agent exists");
        }

        [Fact]
        public void CompPawnAgent_Has_MustBeActive_Reject_Message()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("RimMind.Agent.Gizmo.MustBeActive", content);
            Assert.Contains("MessageTypeDefOf.RejectInput", content);
        }

        [Fact]
        public void CompPawnAgent_Has_AlreadyInMode_Reject_Message()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("RimMind.Agent.Gizmo.AlreadyInMode", content);
        }

        [Fact]
        public void CompPawnAgent_Has_ModeNotApplicable_Reject_Message()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("RimMind.Agent.Gizmo.ModeNotApplicable", content);
        }

        [Fact]
        public void CompPawnAgent_CreateAgent_Success_Uses_PositiveEvent()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("RimMind.Agent.Gizmo.AgentCreated", content);
            Assert.Contains("MessageTypeDefOf.PositiveEvent", content);
        }

        [Fact]
        public void CompPawnAgent_CreateAgent_Failure_Uses_RejectInput()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("RimMind.Agent.Gizmo.CreateAgentFailed", content);
        }

        [Fact]
        public void CompPawnAgent_Inactive_ForceThink_Shows_Reject()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            var elseBranchIdx = content.IndexOf("RimMind.Agent.Gizmo.ForceThinkInactiveDesc");
            Assert.True(elseBranchIdx > 0, "ForceThinkInactiveDesc translation key must exist for inactive agent");
        }

        [Fact]
        public void Window_AgentStateDebug_Accepts_Pawn_Constructor()
        {
            var content = ReadSourceFile(WindowAgentStateDebugRelative);
            Assert.Contains("Window_AgentStateDebug(Pawn? pawn)", content);
            Assert.Contains("_targetPawn", content);
        }

        [Fact]
        public void Window_AgentStateDebug_Has_Parameterless_Constructor_Chain()
        {
            var content = ReadSourceFile(WindowAgentStateDebugRelative);
            Assert.Contains("Window_AgentStateDebug() : this(null)", content);
        }

        [Fact]
        public void Window_AgentStateDebug_Uses_TargetPawn_With_Fallback()
        {
            var content = ReadSourceFile(WindowAgentStateDebugRelative);
            Assert.Contains("_targetPawn", content);
            Assert.Contains("Find.Selector.SingleSelectedThing", content);
        }

        [Fact]
        public void Window_AgentModeDebug_Accepts_Pawn_Constructor()
        {
            var content = ReadSourceFile(WindowAgentModeDebugRelative);
            Assert.Contains("Window_AgentModeDebug(Pawn? pawn)", content);
            Assert.Contains("_initialPawn", content);
        }

        [Fact]
        public void Window_AgentModeDebug_Has_Parameterless_Constructor_Chain()
        {
            var content = ReadSourceFile(WindowAgentModeDebugRelative);
            Assert.Contains("Window_AgentModeDebug() : this(null)", content);
        }

        [Fact]
        public void Window_AgentModeDebug_Uses_InitialPawn_For_Selection()
        {
            var content = ReadSourceFile(WindowAgentModeDebugRelative);
            Assert.Contains("_initialPawn", content);
            Assert.Contains("_cachedPawns.IndexOf(_initialPawn)", content);
        }

        [Fact]
        public void Window_AgentFlowLab_Accepts_Pawn_Constructor()
        {
            var content = ReadSourceFile(WindowAgentFlowLabRelative);
            Assert.Contains("Window_AgentFlowLab(Pawn? pawn)", content);
            Assert.Contains("_initialPawn", content);
        }

        [Fact]
        public void Window_AgentFlowLab_Has_Parameterless_Constructor_Chain()
        {
            var content = ReadSourceFile(WindowAgentFlowLabRelative);
            Assert.Contains("Window_AgentFlowLab() : this(null)", content);
        }

        [Fact]
        public void Window_AgentFlowLab_Uses_InitialPawn_With_Fallback()
        {
            var content = ReadSourceFile(WindowAgentFlowLabRelative);
            Assert.Contains("_initialPawn", content);
            Assert.Contains("Find.Selector.SingleSelectedThing", content);
        }
    }
}
