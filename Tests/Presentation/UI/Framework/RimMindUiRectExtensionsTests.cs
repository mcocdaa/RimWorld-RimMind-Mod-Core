using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Framework;

public sealed class RimMindUiRectExtensionsTests
{
    [Fact]
    public void InsetSafe_NeverProducesNegativeSize()
    {
        Rect rect = new Rect(0f, 0f, 8f, 6f);
        Rect inset = rect.InsetSafe(12f);
        Assert.Equal(4f, inset.x);
        Assert.Equal(3f, inset.y);
        Assert.Equal(1f, inset.width);
        Assert.Equal(1f, inset.height);
    }

    [Fact]
    public void SplitHeaderBody_ReservesGapAndBody()
    {
        Rect rect = new Rect(0f, 0f, 500f, 300f);
        var split = rect.SplitHeaderBody(headerHeight: 30f, gap: 8f);
        Assert.Equal(new Rect(0f, 0f, 500f, 30f), split.Header);
        Assert.Equal(new Rect(0f, 38f, 500f, 262f), split.Body);
    }

    [Fact]
    public void TakeBottom_ReservesBottomBar()
    {
        Rect rect = new Rect(0f, 0f, 500f, 300f);
        var split = rect.TakeBottom(height: 40f, gap: 10f);
        Assert.Equal(new Rect(0f, 0f, 500f, 250f), split.Body);
        Assert.Equal(new Rect(0f, 260f, 500f, 40f), split.Bottom);
    }
}
