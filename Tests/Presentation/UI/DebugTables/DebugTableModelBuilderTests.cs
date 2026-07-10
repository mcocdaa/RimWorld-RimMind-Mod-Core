using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Models.Debug;
using RimMind.Infrastructure.UI.DebugTables;
using Xunit;

namespace RimMind.Tests.Presentation.UI.DebugTables;

public sealed class DebugTableModelBuilderTests
{
    [Fact]
    public void AIRequestsBuilder_MapsRunningAndFailedRows()
    {
        var entries = new List<AIRequestTraceEntry>
        {
            new()
            {
                RequestId = "req-running",
                Source = "Advisor",
                Model = "model-a",
                UserPrompt = "draft plan",
                State = AIRequestTraceState.Running
            },
            new()
            {
                RequestId = "req-failed",
                Source = "Dialogue",
                Model = "model-b",
                Response = "partial response",
                Error = "HTTP timeout after retry",
                ElapsedMs = 42,
                State = AIRequestTraceState.Failed
            }
        };

        DebugTableModel model = AIRequestsDebugTableModelBuilder.Build(entries);

        DebugTableRow running = model.Rows.Single(row => row.Id == "req-running");
        DebugTableRow failed = model.Rows.Single(row => row.Id == "req-failed");
        Assert.Equal(DebugTableStatus.Streaming, running.Status);
        Assert.Equal(DebugTableStatus.Failed, failed.Status);
        Assert.Equal("HTTP timeout after retry", failed.Summary);
        Assert.Equal("42 ms", failed.Duration);
    }

    [Fact]
    public void ToolCallsBuilder_FlattensRequestToolCalls()
    {
        var entries = new List<AIRequestTraceEntry>
        {
            new()
            {
                RequestId = "req-001",
                Source = "Advisor",
                Model = "model-a"
            },
            new()
            {
                RequestId = "req-002",
                Source = "Dialogue",
                Model = "model-b"
            }
        };
        entries[0].ToolCalls.Add(new AIRequestToolCallTrace("tool-ok", "move_to", true, null));
        entries[1].ToolCalls.Add(new AIRequestToolCallTrace("tool-failed", "reserve_target", false, "Target reservation denied"));

        DebugTableModel model = ToolCallsDebugTableModelBuilder.Build(entries);

        Assert.Equal(2, model.Rows.Count);
        DebugTableRow completed = model.Rows.Single(row => row.Id == "tool-ok");
        DebugTableRow failed = model.Rows.Single(row => row.Id == "tool-failed");
        Assert.Equal(DebugTableStatus.Completed, completed.Status);
        Assert.Equal("Advisor", completed.Scope);
        Assert.Equal("move_to", completed.Channel);
        Assert.Equal("model-a", completed.Model);
        Assert.Equal(DebugTableStatus.Failed, failed.Status);
        Assert.Equal("Target reservation denied", failed.Summary);
        Assert.Equal("Dialogue", failed.Scope);
    }
}
