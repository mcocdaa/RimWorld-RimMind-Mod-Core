using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public sealed class DebugTableSnapshotTests
{
    [Fact]
    public void RequestsTableSnapshot_ReservesToolbarHeaderBodyAndBottomBar()
    {
        TablePageLayoutResult layout = TablePageLayout.Calculate(
            new Rect(0f, 0f, 780f, 500f),
            rowCount: 30,
            columnCount: 4);

        Assert.True(layout.Toolbar.y < layout.Header.y);
        Assert.True(layout.Header.y < layout.Body.y);
        Assert.True(layout.BottomBar.y > layout.Body.yMax);
    }
}
