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

        [Fact]
        public void AIRequestsPage_Draws_Full_Prompt_Response_And_ToolCall_Sections()
        {
            string content = ReadSource("Infrastructure/UI/AIRequestsPage/AIRequestsPageDrawer.cs");

            Assert.Contains("entry.SystemPrompt", content);
            Assert.Contains("entry.UserPrompt", content);
            Assert.Contains("entry.AssistantPrompt", content);
            Assert.Contains("entry.Response", content);
            Assert.Contains("entry.ToolCalls", content);
            Assert.Contains("DrawSection", content);
        }

        [Fact]
        public void AIRequestsPage_Truncates_List_Row_Text_But_Not_Detail_Text()
        {
            string content = ReadSource("Infrastructure/UI/AIRequestsPage/AIRequestsPageDrawer.cs");

            Assert.Contains("TruncateForRow", content);
            Assert.Contains("DrawDetail", content);
            Assert.Contains("_detailScrollPosition", content);
        }

        [Fact]
        public void AIRequestsPage_Detail_View_Height_Is_Calculated_From_Sections()
        {
            string content = ReadSource("Infrastructure/UI/AIRequestsPage/AIRequestsPageDrawer.cs");

            Assert.DoesNotContain("1200f", content);
            Assert.Contains("CalculateDetailViewHeight", content);
            Assert.Contains("BuildDetailSections", content);
            Assert.Contains("float contentWidth = CalculateDetailContentWidth(rect.width);", content);
            Assert.Contains("CalculateDetailViewHeight(sections, contentWidth)", content);
            Assert.Contains("Rect view = new(rect.x, rect.y, contentWidth, viewHeight)", content);
            Assert.DoesNotContain("CalculateDetailViewHeight(sections, rect.width - 16f)", content);
            Assert.DoesNotContain("new(rect.x, rect.y, rect.width - 16f", content);
        }

        [Fact]
        public void AIRequestsPage_Detail_Section_Height_Uses_Shared_Text_Helpers()
        {
            string content = ReadSource("Infrastructure/UI/AIRequestsPage/AIRequestsPageDrawer.cs");

            Assert.Contains("ResolveSectionBody", content);
            Assert.Contains("CalculateSectionHeight", content);
            Assert.True(CountOccurrences(content, "CalculateSectionHeight(") >= 3);
            Assert.True(CountOccurrences(content, "ResolveSectionBody(") >= 2);
            Assert.DoesNotContain("Text.CalcHeight(text, view.width)", content);
        }

        private static int CountOccurrences(string content, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = content.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
