using RimMind.Infrastructure.UI.AgentsPage;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI
{
    public sealed class AgentPageLayoutTests
    {
        [Fact]
        public void Calculate_DetailSections_DoNotOverlap()
        {
            var layout = AgentPageLayout.Calculate(new Rect(0f, 0f, 740f, 480f));

            Assert.True(layout.Status.y >= layout.Detail.y);
            Assert.True(layout.Actions.y > layout.Status.yMax);
            Assert.True(layout.Activity.y > layout.Actions.yMax);
            Assert.True(layout.Chat.y > layout.Activity.yMax);
            Assert.Equal(layout.Detail.yMax, layout.Chat.yMax);
        }

        [Fact]
        public void Calculate_SupportedSmallWindow_KeepsMinimumDetailWidth()
        {
            var layout = AgentPageLayout.Calculate(new Rect(0f, 0f, 620f, 420f));

            Assert.True(layout.List.width >= 180f);
            Assert.True(layout.Detail.width >= 360f);
            Assert.True(layout.Activity.height >= 120f);
        }

        [Theory]
        [InlineData(620f, 420f)]
        [InlineData(740f, 480f)]
        [InlineData(980f, 620f)]
        public void Calculate_SupportedViewports_AllMajorRectsStayInsideWindow(float width, float height)
        {
            var root = new Rect(0f, 0f, width, height);
            var layout = AgentPageLayout.Calculate(root);

            Assert.True(root.Contains(new Vector2(layout.List.xMin, layout.List.yMin)));
            Assert.True(root.Contains(new Vector2(layout.List.xMax - 1f, layout.List.yMax - 1f)));
            Assert.True(root.Contains(new Vector2(layout.Detail.xMin, layout.Detail.yMin)));
            Assert.True(root.Contains(new Vector2(layout.Detail.xMax - 1f, layout.Detail.yMax - 1f)));
            Assert.True(root.Contains(new Vector2(layout.Chat.xMax - 1f, layout.Chat.yMax - 1f)));
        }
    }
}
