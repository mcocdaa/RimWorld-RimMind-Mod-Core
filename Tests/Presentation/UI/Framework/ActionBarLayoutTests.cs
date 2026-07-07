using System.Linq;
using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Framework;

public sealed class ActionBarLayoutTests
{
    [Fact]
    public void Calculate_WrapsButtonsWhenNarrow()
    {
        var layout = ActionBarLayout.Calculate(new Rect(0f, 0f, 260f, 90f), new[] { "Pause", "ForceThink", "OpenRequests" });

        Assert.True(layout.RowCount >= 2);
        Assert.All(layout.Buttons, b => Assert.True(b.Rect.width >= RimMindUiMetrics.ButtonMinWidth));
        Assert.True(layout.Buttons.Last().Rect.y > layout.Buttons.First().Rect.y);
    }

    [Fact]
    public void Calculate_UsesSingleRowWhenWide()
    {
        var layout = ActionBarLayout.Calculate(new Rect(0f, 0f, 520f, 40f), new[] { "Pause", "ForceThink", "OpenRequests" });

        Assert.Equal(1, layout.RowCount);
        Assert.Equal(3, layout.Buttons.Count);
    }
}
