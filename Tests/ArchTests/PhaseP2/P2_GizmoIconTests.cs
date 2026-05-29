using System.IO;
using System;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP2
{
    public class P2_GizmoIconTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private const string CompPawnAgentRelative = "Source/Infrastructure/Verse/CompPawnAgent.cs";

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(ProjectRoot, relativePath));

        [Fact]
        public void CompPawnAgent_No_Generic_AgentIcon_Remains()
        {
            var path = Path.Combine(ProjectRoot, CompPawnAgentRelative);
            Assert.True(File.Exists(path), $"File must exist: {path}");
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.DoesNotContain("\"UI/AgentIcon\"", content);
        }

        [Fact]
        public void CompPawnAgent_AgentState_Gizmo_Uses_State_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentStateIcon", content);
        }

        [Fact]
        public void CompPawnAgent_Pause_Gizmo_Uses_Pause_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentPauseIcon", content);
        }

        [Fact]
        public void CompPawnAgent_ForceThink_Gizmo_Uses_Think_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentThinkIcon", content);
        }

        [Fact]
        public void CompPawnAgent_Dialogue_Gizmo_Uses_Dialogue_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentDialogueIcon", content);
        }

        [Fact]
        public void CompPawnAgent_Mode_Gizmo_Uses_Mode_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentModeIcon", content);
        }

        [Fact]
        public void CompPawnAgent_EmergencyStop_Gizmo_Uses_Stop_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentStopIcon", content);
        }

        [Fact]
        public void CompPawnAgent_Resume_Gizmo_Uses_Resume_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentResumeIcon", content);
        }

        [Fact]
        public void CompPawnAgent_DevView_Gizmo_Uses_Dev_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentDevIcon", content);
        }
    }
}
