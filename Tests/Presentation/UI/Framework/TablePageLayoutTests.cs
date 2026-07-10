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

    [Fact]
    public void CalculateColumnRect_UsesSharedContentWidthForHeaderAndBody()
    {
        TablePageLayoutResult layout = TablePageLayout.Calculate(
            new Rect(0f, 0f, 300f, 400f),
            rowCount: 20,
            columnCount: 8);

        Rect headerCell = TablePageLayout.CalculateColumnRect(
            layout.ViewRect.width,
            columnIndex: 1,
            columnCount: 8,
            y: 0f,
            height: layout.Header.height,
            horizontalScroll: 0f,
            padding: 6f);
        Rect bodyCell = TablePageLayout.CalculateColumnRect(
            layout.ViewRect.width,
            columnIndex: 1,
            columnCount: 8,
            y: 0f,
            height: RimMindUiMetrics.DebugRowHeight,
            horizontalScroll: 0f,
            padding: 6f);

        Assert.Equal(headerCell.x, bodyCell.x);
        Assert.Equal(headerCell.width, bodyCell.width);
        Assert.Equal(126f, headerCell.x);
    }

    [Fact]
    public void CalculateColumnRect_OffsetsHeaderByHorizontalScroll()
    {
        Rect cell = TablePageLayout.CalculateColumnRect(
            contentWidth: 960f,
            columnIndex: 2,
            columnCount: 8,
            y: 0f,
            height: 26f,
            horizontalScroll: 120f,
            padding: 6f);

        Assert.Equal(126f, cell.x);
    }

    [Fact]
    public void CalculateVisibleRowRange_OnlyIncludesViewportRows()
    {
        TableVisibleRowRange range = TablePageLayout.CalculateVisibleRowRange(
            rowCount: 200,
            scrollY: 260f,
            viewportHeight: 104f,
            rowHeight: 26f);

        Assert.Equal(10, range.FirstIndex);
        Assert.Equal(14, range.LastExclusive);
    }

    private static void AssertContained(Rect root, Rect rect)
    {
        Assert.True(rect.width >= 0f);
        Assert.True(rect.height >= 0f);
        Assert.True(root.ContainsRect(rect));
    }
}
