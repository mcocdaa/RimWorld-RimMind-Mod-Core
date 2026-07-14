using System;
using System.IO;
using System.Linq;
using RimMind.Infrastructure.UI.DebugCenter;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP7
{
    public class P7B_DebugCenterShellTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");
        private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(SourceDir, ".."));

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string ReadRepo(string relativePath)
            => File.ReadAllText(Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        [Fact]
        public void DebugCenter_Has_Registerable_Default_Pages()
        {
            string registry = ReadSource("Infrastructure/UI/DebugCenter/DebugCenterPageRegistry.cs");

            Assert.Contains("() => new OverviewDebugCenterPageDrawer", registry);
            Assert.Contains("() => new AgentsDebugCenterPageDrawer", registry);
            Assert.Contains("() => new AIRequestsDebugCenterPageDrawer", registry);
            Assert.Contains("() => new ToolCallsDebugCenterPageDrawer", registry);
            Assert.Contains("() => new MechanismsDebugCenterPageDrawer", registry);
            Assert.Contains("() => new ContextKeysDebugCenterPageDrawer", registry);
        }

        [Fact]
        public void DebugCenter_Default_Constructor_Uses_Registry_Default()
        {
            string hub = ReadSource("Infrastructure/UI/MainTabWindow_RimMindHub.cs");
            var pages = DebugCenterPageRegistry.GetAll();

            Assert.Contains("public Window_RimMindHub()", hub);
            Assert.Contains("DebugCenterPageRegistry.DefaultPageId", hub);
            Assert.Contains("DebugCenterPageRegistry.CreateAllRegistrations()", hub);
            Assert.False(pages.Single(page => page.Id == "ai_requests").IsDefault);
            Assert.True(pages.Single(page => page.Id == "overview").IsDefault);
        }

        [Fact]
        public void DebugCenter_Has_Deep_Link_Factories()
        {
            string content = ReadSource("Infrastructure/UI/MainTabWindow_RimMindHub.cs");

            Assert.Contains("OpenAgentsForPawn", content);
            Assert.Contains("OpenAIRequests", content);
            Assert.Contains("Pawn? selectedPawn", content);
        }

        [Fact]
        public void CoreIcon_CtrlClick_Opens_AIRequests_DebugCenter_Page()
        {
            string content = ReadSource("Infrastructure/Patches/RimMindPlaySettingsPatch.cs");

            Assert.Contains("Window_RimMindHub.OpenAIRequests()", content);
            Assert.DoesNotContain("new Window_RimMindHub()", content);
        }

        [Fact]
        public void DebugCenter_Tab_Labels_Are_Localized()
        {
            string en = ReadRepo("Languages/English/Keyed/RimMind_Core.xml");
            string zh = ReadRepo("Languages/ChineseSimplified/Keyed/RimMind_Core.xml");

            string[] keys =
            {
                "RimMind.UI.Hub.Tab.Overview",
                "RimMind.UI.Hub.Tab.Agents",
                "RimMind.UI.Hub.Tab.AIRequests",
                "RimMind.UI.Hub.Tab.ToolCalls",
                "RimMind.UI.Hub.Tab.Mechanisms",
                "RimMind.UI.Hub.Tab.ContextKeys"
            };

            foreach (string key in keys)
            {
                Assert.Contains($"<{key}>", en);
                Assert.Contains($"<{key}>", zh);
            }
        }

        [Fact]
        public void RimMindUI_ScrollView_Returns_ViewRect_For_Content_Coordinates()
        {
            string content = ReadSource("Infrastructure/UI/RimMindUI.cs");

            Assert.Contains("Rect viewRect = new Rect", content);
            Assert.Contains("return (viewRect, viewRect);", content);
            Assert.DoesNotContain("return (rect, viewRect);", content);
        }
    }
}
