using System.Linq;
using RimMind.Infrastructure.UI.DebugCenter.Overview;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public sealed class DebugCenterOptimizationSnapshotTests
{
    [Fact]
    public void SnapshotCases_IncludeDebugOverview()
    {
        Assert.Contains(UiSnapshotCases.All(), document => document.Id == "debug_overview");
    }

    [Theory]
    [InlineData("requests_mixed_status")]
    [InlineData("toolcalls_mixed_status")]
    [InlineData("context_keys_dense")]
    public void SnapshotCases_IncludeRequiredDebugTableSnapshots(string id)
    {
        Assert.Contains(UiSnapshotCases.All(), document => document.Id == id);
    }

    [Fact]
    public void RequestsSnapshot_UsesRuntimeWindowAndSplitListGeometry()
    {
        var document = UiSnapshotCases.All().Single(item => item.Id == "requests_mixed_status");
        var list = document.Elements.Single(element => element.Name == "request_list");
        var detail = document.Elements.Single(element => element.Name == "request_detail");

        Assert.Equal(780f, document.Viewport.width);
        Assert.InRange(list.Rect.width, 240f, 300f);
        Assert.True(list.Rect.xMax < detail.Rect.x);
    }

    [Fact]
    public void DebugOverviewSnapshot_ContainsFourSummaryCards()
    {
        var document = UiSnapshotCases.All().Single(item => item.Id == "debug_overview");

        Assert.Contains(document.Elements, element => element.Name == "overview_health");
        Assert.Contains(document.Elements, element => element.Name == "overview_agents");
        Assert.Contains(document.Elements, element => element.Name == "overview_queue");
        Assert.Contains(document.Elements, element => element.Name == "overview_selection");
    }

    [Fact]
    public void DebugCenterOverviewModel_DerivesAgentSummaryFromCounts()
    {
        var model = new DebugCenterOverviewModel(
            activeAgents: 2,
            pausedAgents: 1,
            pendingAgents: 3,
            errorAgents: 4,
            pendingRequests: 5,
            queueState: "Running",
            selectedObject: "Nickie");

        Assert.Equal("2 active / 1 paused / 3 pending / 4 error", model.AgentSummary);
    }
}
