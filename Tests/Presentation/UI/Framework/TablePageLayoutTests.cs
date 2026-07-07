using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Framework;

public sealed class TablePageLayoutTests
{
    [Fact]
    public void Calculate_ReservesToolbarHeaderBodyAndBottomBar()
    {
        var layout = TablePageLayout.Calculate(new Rect(0f, 0f, 800f, 500f), rowCount: 40, columnCount: 4);

        Assert.True(layout.Toolbar.y < layout.Header.y);
        Assert.True(layout.Header.y < layout.Body.y);
        Assert.True(layout.BottomBar.y > layout.Body.yMax);
        Assert.True(layout.ViewRect.height > layout.Body.height);
    }

    [Fact]
    public void Calculate_TinyHeight_KeepsMajorRectsContainedAndNonNegative()
    {
        Rect root = new Rect(0f, 0f, 120f, 40f);
        var layout = TablePageLayout.Calculate(root, rowCount: 2, columnCount: 2);

        AssertContained(root, layout.Toolbar);
        AssertContained(root, layout.Header);
        AssertContained(root, layout.Body);
        AssertContained(root, layout.BottomBar);
    }

    private static void AssertContained(Rect root, Rect rect)
    {
        Assert.True(rect.width >= 0f);
        Assert.True(rect.height >= 0f);
        Assert.True(root.ContainsRect(rect));
    }
}
