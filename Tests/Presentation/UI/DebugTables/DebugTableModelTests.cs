using RimMind.Infrastructure.UI.DebugTables;
using Xunit;

namespace RimMind.Tests.Presentation.UI.DebugTables;

public sealed class DebugTableModelTests
{
    [Fact]
    public void RequestFixture_ContainsRequiredStatuses()
    {
        var table = DebugTableFixtures.MixedRequests();

        Assert.Contains(table.Rows, row => row.Status == DebugTableStatus.Waiting);
        Assert.Contains(table.Rows, row => row.Status == DebugTableStatus.Streaming);
        Assert.Contains(table.Rows, row => row.Status == DebugTableStatus.Completed);
        Assert.Contains(table.Rows, row => row.Status == DebugTableStatus.Failed);
        Assert.Contains(table.Rows, row => row.Summary.Contains("ToolCall"));
    }

    [Fact]
    public void ToolCallFixture_ContainsFailedToolCall()
    {
        var table = DebugTableFixtures.MixedToolCalls();

        Assert.Contains(table.Rows, row => row.Status == DebugTableStatus.Failed);
    }

    [Fact]
    public void Rows_ExposeStableStatusColorNames()
    {
        Assert.Equal("orange", DebugTableRow.Create("1", DebugTableStatus.Waiting, "10:00", "Pawn", "Nickie", "chat", "deepseek-chat", "waiting", "0ms").StatusColorName);
        Assert.Equal("blue", DebugTableRow.Create("2", DebugTableStatus.Streaming, "10:00", "Pawn", "Nickie", "chat", "deepseek-chat", "streaming", "1s").StatusColorName);
        Assert.Equal("green", DebugTableRow.Create("3", DebugTableStatus.Completed, "10:00", "Pawn", "Nickie", "chat", "deepseek-chat", "done", "2s").StatusColorName);
        Assert.Equal("red", DebugTableRow.Create("4", DebugTableStatus.Failed, "10:00", "Pawn", "Nickie", "chat", "deepseek-chat", "failed", "2s").StatusColorName);
        Assert.Equal("gray", DebugTableRow.Create("5", DebugTableStatus.Cancelled, "10:00", "Pawn", "Nickie", "chat", "deepseek-chat", "cancelled", "0ms").StatusColorName);
    }
}
