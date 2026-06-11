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

        [Fact]
        public void Hub_Delegates_Agents_Page_To_AgentsPageDrawer()
        {
            string content = ReadSource("Infrastructure/UI/MainTabWindow_RimMindHub.cs");

            Assert.Contains("AgentsPageDrawer", content);
            Assert.Contains("_agentsPage.Draw", content);
            Assert.Contains("_selectedPawn", content);
        }

        [Fact]
        public void AgentsPageDrawer_Has_Create_Pause_Resume_Activate_And_Chat()
        {
            string content = ReadSource("Infrastructure/UI/AgentsPage/AgentsPageDrawer.cs");

            Assert.Contains("RimMind.UI.AgentsPage.CreateStart", content);
            Assert.Contains("RimMind.UI.AgentsPage.Activate", content);
            Assert.Contains("RimMind.UI.AgentsPage.Restart", content);
            Assert.Contains("SafeTransitionTo", content);
            Assert.Contains("_chatDraft", content);
            Assert.Contains("SendAgentMessage", content);
        }

        [Fact]
        public void CompPawnAgent_Has_EnsureAgentCreated()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");

            Assert.Contains("EnsureAgentCreated", content);
        }
    }
}
