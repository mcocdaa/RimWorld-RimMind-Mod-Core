using System.Linq;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.UI.AgentsPage;
using Xunit;

namespace RimMind.Tests.Presentation.UI;

public sealed class AgentWorkbenchViewModelTests
{
    [Fact]
    public void PendingCreation_DisablesChatAndOnlyAllowsCreateStart()
    {
        var model = AgentPageViewModel.PendingCreation("Cashton");

        Assert.True(model.IsPendingCreation);
        Assert.False(model.CanChat);
        Assert.Equal(new[] { AgentPageAction.CreateStart }, model.Actions);
    }

    [Fact]
    public void FromState_Active_AllowsPauseForceThinkAndRequests()
    {
        var model = AgentPageViewModel.FromState("Nickie", AgentState.Active, pendingRequests: 1, requestRows: 0);

        Assert.True(model.CanChat);
        Assert.Contains(AgentPageAction.Pause, model.Actions);
        Assert.Contains(AgentPageAction.ForceThink, model.Actions);
        Assert.Contains(AgentPageAction.OpenRequests, model.Actions);
    }

    [Fact]
    public void WorkbenchEvents_KeepWaitingStreamingSuccessAndErrorRows()
    {
        var model = AgentPageViewModel.FromState(
            "Nickie",
            AgentState.Active,
            pendingRequests: 2,
            requestRows: 4,
            traceRows: new[]
            {
                AgentRequestTraceRow.Waiting("request created"),
                AgentRequestTraceRow.Streaming("streaming response"),
                AgentRequestTraceRow.Success("toolcall: move_to_cell", "accepted"),
                AgentRequestTraceRow.Error("toolcall: equip", "failed", "tool failed")
            });

        Assert.Equal(4, model.TraceRows.Count);
        Assert.Contains(model.TraceRows, row => row.Status == AgentRequestTraceStatus.Waiting || row.Status == AgentRequestTraceStatus.Pending);
        Assert.Contains(model.TraceRows, row => row.Status == AgentRequestTraceStatus.Streaming);
        Assert.Contains(model.TraceRows, row => row.Status == AgentRequestTraceStatus.Success);
        Assert.Contains(model.TraceRows, row => row.Status == AgentRequestTraceStatus.Error);
    }

    [Fact]
    public void AgentListItem_ReportsLifecycleGroup()
    {
        var pending = AgentListItem.PendingPawn("pawn-cashton", "Cashton");
        var active = AgentListItem.ExistingPawn("pawn-nickie", "Nickie", AgentState.Active);
        var paused = AgentListItem.ExistingPawn("pawn-paused", "Paused Pawn", AgentState.Paused);
        var error = AgentListItem.ErrorPawn("pawn-error", "Broken Pawn", "last request failed");

        Assert.Equal(AgentLifecycleGroup.Pending, pending.Group);
        Assert.Equal(AgentLifecycleGroup.Active, active.Group);
        Assert.Equal(AgentLifecycleGroup.Paused, paused.Group);
        Assert.Equal(AgentLifecycleGroup.Error, error.Group);
    }

    [Fact]
    public void AgentListItem_PawnFactories_ReportPawnScope()
    {
        var active = AgentListItem.ExistingPawn("pawn-nickie", "Nickie", AgentState.Active);
        var pending = AgentListItem.PendingPawn("pawn-cashton", "Cashton");
        var error = AgentListItem.ErrorPawn("pawn-error", "Broken Pawn", "last request failed");

        Assert.Equal("Pawn", active.ScopeLabel);
        Assert.Equal("Pawn", pending.ScopeLabel);
        Assert.Equal("Pawn", error.ScopeLabel);
    }
}
