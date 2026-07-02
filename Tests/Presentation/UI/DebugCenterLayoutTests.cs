using RimMind.Infrastructure.UI.DebugCenter;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI
{
    public sealed class DebugCenterLayoutTests
    {
        [Fact]
        public void CalculateHub_Separates_Header_Tabs_And_Content()
        {
            var layout = DebugCenterLayout.CalculateHub(new Rect(0f, 0f, 780f, 580f));

            Assert.True(layout.Header.y >= layout.Body.y);
            Assert.True(layout.Tabs.y > layout.Header.yMax);
            Assert.True(layout.Content.y > layout.Tabs.yMax);
            Assert.True(layout.Content.height > 400f);
        }

        [Fact]
        public void CalculateAgentPage_Keeps_List_And_Detail_From_Overlapping()
        {
            var layout = DebugCenterLayout.CalculateAgentPage(new Rect(0f, 0f, 740f, 480f));

            Assert.True(layout.List.width >= 220f);
            Assert.True(layout.Detail.x > layout.List.xMax);
            Assert.True(layout.Detail.width >= 360f);
        }

        [Fact]
        public void CalculateAgentPage_Keeps_Chat_At_Bottom_Below_Activity()
        {
            var layout = DebugCenterLayout.CalculateAgentPage(new Rect(0f, 0f, 740f, 480f));

            Assert.True(layout.Actions.y > layout.Header.yMax);
            Assert.True(layout.Activity.y > layout.Actions.yMax);
            Assert.True(layout.Chat.y > layout.Activity.yMax);
            Assert.Equal(layout.Detail.yMax, layout.Chat.yMax);
        }
    }
}
