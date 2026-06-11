using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP7
{
    public class P7E_AIRequestsPageSourceTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        [Fact]
        public void AIRequestsPage_Drawer_Uses_State_Colors_And_Details()
        {
            string content = ReadSource("Infrastructure/UI/AIRequestsPage/AIRequestsPageDrawer.cs");

            Assert.Contains("AIRequestTraceState.Running", content);
            Assert.Contains("AIRequestTraceState.Completed", content);
            Assert.Contains("AIRequestTraceState.Failed", content);
            Assert.Contains("TooltipHandler.TipRegion", content);
            Assert.Contains("DrawDetail", content);
            Assert.Contains("ToolCalls", content);
            Assert.Contains("BeginScrollView", content);
            Assert.Contains("StateLabelFor", content);
            Assert.Contains("DrawHighlight", content);
            Assert.Contains("AIRequestsPage.Empty", content);
        }
    }
}
