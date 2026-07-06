using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP7
{
    public class P7D_AgentsPageSourceTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string ReadAgentsPageSources()
            => string.Join(Environment.NewLine, new[]
            {
                ReadSource("Infrastructure/UI/AgentsPage/AgentsPageDrawer.cs"),
                ReadSource("Infrastructure/UI/AgentsPage/AgentDetailPanelDrawer.cs"),
                ReadSource("Infrastructure/UI/AgentsPage/AgentChatPanelDrawer.cs"),
                ReadSource("Infrastructure/UI/AgentsPage/AgentListPanelDrawer.cs"),
                ReadSource("Infrastructure/UI/AgentsPage/AgentActivityStreamDrawer.cs")
            });

        [Fact]
        public void Hub_Delegates_Agents_Page_To_AgentsPageDrawer()
        {
            string content = ReadSource("Infrastructure/UI/MainTabWindow_RimMindHub.cs");
            string page = ReadSource("Infrastructure/UI/DebugCenter/Pages/AgentsDebugCenterPageDrawer.cs");

            Assert.Contains("DebugCenterPageRegistry.CreateAll()", content);
            Assert.Contains("AgentsPageDrawer", page);
            Assert.Contains("_drawer.Draw", page);
            Assert.Contains("context.SelectedPawn", page);
            Assert.DoesNotContain("DebugCenterPageRegistry.SelectedPawn", page);
            Assert.Contains("_selectedPawn", content);
        }

        [Fact]
        public void AgentsPageDrawer_Has_Create_Pause_Resume_Activate_And_Chat()
        {
            string content = ReadAgentsPageSources();

            Assert.Contains("RimMind.UI.AgentsPage.CreateStart", content);
            Assert.Contains("RimMind.UI.AgentsPage.Activate", content);
            Assert.Contains("RimMind.UI.AgentsPage.Restart", content);
            Assert.Contains("SafeTransitionTo", content);
            Assert.Contains("_chatDraft", content);
            Assert.Contains("SendAgentMessage", content);
        }

        [Fact]
        public void AgentsPageDrawer_Does_Not_Fake_Send_Chat_Messages()
        {
            string content = ReadSource("Infrastructure/UI/AgentsPage/AgentChatPanelDrawer.cs");
            string sendMethod = content.Substring(content.IndexOf("private static void SendAgentMessage", StringComparison.Ordinal));

            Assert.Contains("RimMind.UI.AgentsPage.MessageUnavailable", sendMethod);
            Assert.Contains("MessageTypeDefOf.RejectInput", sendMethod);
            Assert.DoesNotContain("RimMind.UI.AgentsPage.MessageSent", sendMethod);
            Assert.DoesNotContain("_chatDraft = string.Empty", sendMethod);
        }

        [Fact]
        public void AgentsPageDrawer_ActivePaused_Detail_Can_Open_AIRequests()
        {
            string content = ReadSource("Infrastructure/UI/AgentsPage/AgentDetailPanelDrawer.cs");

            Assert.Contains("RimMind.UI.AgentsPage.OpenRequests", content);
            Assert.Contains("Window_RimMindHub.OpenAIRequests()", content);
            Assert.Contains("Find.WindowStack.Add", content);
        }

        [Fact]
        public void CompPawnAgent_Has_EnsureAgentCreated()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");

            Assert.Contains("EnsureAgentCreated", content);
        }

        [Fact]
        public void AgentFlowLab_Does_Not_Expose_Storyteller_Scope()
        {
            string content = ReadSource("Infrastructure/UI/Window_AgentFlowLab.cs");

            Assert.DoesNotContain("AgentFlowScope.Storyteller", content);
            Assert.DoesNotContain("RimMind.UI.AgentFlowLab.ScopeStoryteller", content);
            Assert.DoesNotContain("Find.Storyteller", content);
        }

        [Fact]
        public void AgentsPage_IsSplitIntoFocusedPanelDrawers()
        {
            Assert.True(File.Exists(Path.Combine(SourceDir, "Infrastructure", "UI", "AgentsPage", "AgentListPanelDrawer.cs")));
            Assert.True(File.Exists(Path.Combine(SourceDir, "Infrastructure", "UI", "AgentsPage", "AgentDetailPanelDrawer.cs")));
            Assert.True(File.Exists(Path.Combine(SourceDir, "Infrastructure", "UI", "AgentsPage", "AgentActivityStreamDrawer.cs")));
            Assert.True(File.Exists(Path.Combine(SourceDir, "Infrastructure", "UI", "AgentsPage", "AgentChatPanelDrawer.cs")));
        }

        [Fact]
        public void AgentsPageDrawer_Records_Internal_LayoutScope()
        {
            string content = ReadSource("Infrastructure/UI/AgentsPage/AgentsPageDrawer.cs");

            Assert.Contains("RimMindLayoutScope scope", content);
            Assert.Contains("scope.Record(layout.List", content);
            Assert.Contains("scope.Record(layout.Detail", content);
            Assert.Contains("_listDrawer.Draw(", content);
            Assert.Contains("_detailDrawer.Draw(", content);
        }
    }
}
