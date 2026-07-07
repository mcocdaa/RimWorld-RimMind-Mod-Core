using System.Collections.Generic;
using System.Linq;
using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Framework;

public sealed class TabbedPageLayoutTests
{
    [Fact]
    public void Calculate_WideWindow_UsesSingleRowAndContentBelowTabs()
    {
        var tabs = MakeTabs(6);
        var layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 900f, 580f), tabs);
        Assert.Equal(1, layout.RowCount);
        Assert.Equal(6, layout.TabRects.Count);
        Assert.True(layout.Content.y > layout.TabBar.yMax);
        Assert.All(layout.TabRects, tab => Assert.True(layout.TabBar.ContainsRect(tab.Rect)));
    }

    [Fact]
    public void Calculate_NarrowWindow_WrapsTabsAndKeepsReadableWidth()
    {
        var tabs = MakeTabs(8);
        var layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 560f, 580f), tabs);
        Assert.True(layout.RowCount >= 2);
        Assert.All(layout.TabRects, tab => Assert.True(tab.Rect.width >= RimMindUiMetrics.TabMinWidth - 1f));
        Assert.True(layout.Content.y >= layout.TabBar.yMax + RimMindUiMetrics.SectionGap);
    }

    [Fact]
    public void Calculate_SelectedTabIsPreserved()
    {
        var tabs = MakeTabs(4, selected: "tab2");
        var layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 640f, 400f), tabs);
        Assert.Equal("tab2", layout.TabRects.Single(t => t.Selected).Id);
    }

    [Fact]
    public void Calculate_EmptyTabs_ReturnsEmptyTabRectsAndContentArea()
    {
        var layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 320f, 180f), new List<TabbedPageTabModel>());
        Assert.Equal(1, layout.RowCount);
        Assert.Empty(layout.TabRects);
        Assert.True(layout.Content.height >= 1f);
        Assert.True(layout.Body.ContainsRect(layout.TabBar));
    }

    [Fact]
    public void Calculate_TinyRect_KeepsAllAreasContainedAndNonNegative()
    {
        var tabs = MakeTabs(2);
        var layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 10f, 10f), tabs);

        AssertLayoutContainedAndNonNegative(layout);
    }

    [Fact]
    public void Calculate_ManyTabsInShortRect_KeepsAllAreasContainedAndNonNegative()
    {
        var tabs = MakeTabs(30);
        var layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 320f, 80f), tabs);

        AssertLayoutContainedAndNonNegative(layout);
    }

    [Fact]
    public void Calculate_DisabledTabIsPreserved()
    {
        var tabs = MakeTabs(4, disabled: "tab2");
        var layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 640f, 400f), tabs);

        Assert.False(layout.TabRects.Single(t => t.Id == "tab2").Enabled);
    }

    private static IReadOnlyList<TabbedPageTabModel> MakeTabs(int count, string selected = "tab0", string? disabled = null)
        => Enumerable.Range(0, count)
            .Select(i => new TabbedPageTabModel($"tab{i}", $"Label {i}", $"Label.Key.{i}", selected == $"tab{i}", disabled != $"tab{i}", null))
            .ToList();

    private static void AssertLayoutContainedAndNonNegative(TabbedPageLayoutResult layout)
    {
        AssertNonNegative(layout.Body);
        AssertNonNegative(layout.TabBar);
        AssertNonNegative(layout.Content);
        Assert.True(layout.Body.ContainsRect(layout.TabBar));
        Assert.True(layout.Body.ContainsRect(layout.Content));

        Assert.All(layout.TabRects, tab =>
        {
            AssertNonNegative(tab.Rect);
            Assert.True(layout.Body.ContainsRect(tab.Rect));
            Assert.True(layout.TabBar.ContainsRect(tab.Rect));
        });
    }

    private static void AssertNonNegative(Rect rect)
    {
        Assert.True(rect.width >= 0f);
        Assert.True(rect.height >= 0f);
    }
}
