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
    }
}
