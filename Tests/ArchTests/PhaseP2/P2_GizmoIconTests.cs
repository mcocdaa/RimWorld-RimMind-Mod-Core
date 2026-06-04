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
        public void CompPawnAgent_Uses_Independent_Agent_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("\"UI/AgentIcon\"", content);
        }

        [Fact]
        public void CompPawnAgent_Does_Not_Use_Brand_Icon_For_Gizmo()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.DoesNotContain("\"UI/RimMind/Icon\"", content);
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

        [Fact]
        public void AgentIcon_Texture_File_Exists()
        {
            var path = Path.Combine(ProjectRoot, "Textures", "UI", "AgentIcon.png");
            Assert.True(File.Exists(path), $"Agent icon texture must exist: {path}");
        }

        [Fact]
        public void All_Referenced_Gizmo_Icon_Textures_Exist()
        {
            var iconNames = new[]
            {
                "AgentIcon", "AgentStateIcon", "AgentPauseIcon",
                "AgentThinkIcon", "AgentDialogueIcon", "AgentModeIcon",
                "AgentStopIcon", "AgentResumeIcon", "AgentDevIcon"
            };
            foreach (var name in iconNames)
            {
                var path = Path.Combine(ProjectRoot, "Textures", "UI", $"{name}.png");
                Assert.True(File.Exists(path), $"Gizmo icon texture must exist: {path}");
            }
        }

        [Fact]
        public void Brand_Icon_Texture_Exists_Separately()
        {
            var path = Path.Combine(ProjectRoot, "Textures", "UI", "RimMind", "Icon.png");
            Assert.True(File.Exists(path), $"Brand icon texture must exist separately from Agent icon: {path}");
        }

        [Fact]
        public void Brand_Icon_Used_Only_In_Overlay_Not_In_Gizmo()
        {
            var overlayPath = Path.Combine(ProjectRoot, "Source", "Infrastructure", "Patches", "RimMindPlaySettingsPatch.cs");
            Assert.True(File.Exists(overlayPath), $"Overlay file must exist: {overlayPath}");
            var overlayContent = File.ReadAllText(overlayPath);
            Assert.Contains("UI/RimMind/Icon", overlayContent);

            var gizmoContent = ReadSourceFile(CompPawnAgentRelative);
            Assert.DoesNotContain("UI/RimMind/Icon", gizmoContent);
        }

        [Fact]
        public void AgentIcon_Has_Fallback_To_BadTex()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            var agentIconLine = content.IndexOf("\"UI/AgentIcon\"");
            Assert.True(agentIconLine > 0, "UI/AgentIcon reference must exist");
            var nearby = content.Substring(agentIconLine, Math.Min(200, content.Length - agentIconLine));
            Assert.Contains("BaseContent.BadTex", nearby);
        }
    }
}
