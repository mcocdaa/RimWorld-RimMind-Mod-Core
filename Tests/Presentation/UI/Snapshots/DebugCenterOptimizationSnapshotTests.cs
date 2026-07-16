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
    public void DebugOverviewSnapshot_ContainsRuntimeLoopDiagnosticsAndQuickActionsWithinViewport()
    {
        var document = UiSnapshotCases.All().Single(item => item.Id == "debug_overview");

        Assert.Contains(document.Elements, element => element.Name == "overview_agent_loop");
        Assert.Contains(document.Elements, element => element.Name == "overview_last_loop_tick");
        Assert.Contains(document.Elements, element => element.Name == "overview_loop_faults");
        Assert.Contains(document.Elements, element => element.Name == "overview_quick_actions");
        Assert.Contains(document.Elements, element => element.Name == "overview_nav_agents");
        Assert.Contains(document.Elements, element => element.Name == "overview_nav_ai_requests");
        Assert.Contains(document.Elements, element => element.Name == "overview_nav_tool_calls");
        Assert.Contains(document.Elements, element => element.Name == "overview_nav_mechanisms");
        Assert.All(document.Elements, element => Assert.True(
            element.Rect.yMax <= document.Viewport.yMax,
            $"{element.Name} extends below the overview viewport."));
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
            selectedObject: "Nickie",
            registeredPawnAgents: 4,
            registeredScopedAgents: 2,
            lastAgentLoopTick: 900,
            agentLoopFaults: 1);

        Assert.Equal("2 active / 1 paused / 3 pending / 4 error", model.AgentSummary);
        Assert.Equal("4 pawn / 2 scoped", model.AgentLoopSummary);
        Assert.Equal(900, model.LastAgentLoopTick);
        Assert.Equal(1, model.AgentLoopFaults);
    }
}
