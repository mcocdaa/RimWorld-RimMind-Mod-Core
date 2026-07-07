using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Framework;

public sealed class SplitPageLayoutTests
{
    [Theory]
    [InlineData(620f, 420f)]
    [InlineData(780f, 580f)]
    [InlineData(1100f, 720f)]
    public void Calculate_KeepsListAndDetailReadable(float width, float height)
    {
        var layout = SplitPageLayout.Calculate(new Rect(0f, 0f, width, height), 0.28f, 180f, 280f, 360f);

        Assert.True(layout.List.width >= 180f);
        Assert.True(layout.Detail.width >= 360f);
        Assert.True(layout.Root.ContainsRect(layout.List));
        Assert.True(layout.Root.ContainsRect(layout.Detail));
        Assert.True(layout.Detail.x > layout.List.xMax);
    }

    [Fact]
    public void Calculate_TinyWidth_KeepsPanesContainedAndNonNegative()
    {
        var layout = SplitPageLayout.Calculate(new Rect(0f, 0f, 100f, 60f), 0.28f, 180f, 280f, 360f);

        Assert.True(layout.List.width >= 0f);
        Assert.True(layout.Detail.width >= 0f);
        Assert.True(layout.Root.ContainsRect(layout.List));
        Assert.True(layout.Root.ContainsRect(layout.Detail));
    }
}
