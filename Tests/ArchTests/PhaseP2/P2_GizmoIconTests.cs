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
        public void CompPawnAgent_DevView_Gizmo_Uses_Dev_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("UI/AgentDevIcon", content);
        }

        [Fact]
        public void CompPawnAgent_OldIndividualGizmoIcons_Removed()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            // Old individual gizmo icons have been moved into Debug Center Agents page
            Assert.DoesNotContain("UI/AgentStateIcon", content);
            Assert.DoesNotContain("UI/AgentPauseIcon", content);
            Assert.DoesNotContain("UI/AgentThinkIcon", content);
            Assert.DoesNotContain("UI/AgentDialogueIcon", content);
            Assert.DoesNotContain("UI/AgentModeIcon", content);
            Assert.DoesNotContain("UI/AgentStopIcon", content);
            Assert.DoesNotContain("UI/AgentResumeIcon", content);
        }

        [Fact]
        public void AgentIcon_Texture_File_Exists()
        {
            var path = Path.Combine(ProjectRoot, "Textures", "UI", "AgentIcon.png");
            Assert.True(File.Exists(path), $"Agent icon texture must exist: {path}");
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
