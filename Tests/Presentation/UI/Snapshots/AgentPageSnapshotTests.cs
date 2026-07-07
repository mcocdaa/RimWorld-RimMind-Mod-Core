using RimMind.Infrastructure.UI.AgentsPage;
using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public sealed class AgentPageSnapshotTests
{
    [Fact]
    public void AgentActiveSnapshot_HasReadableSplitAndBottomChat()
    {
        var layout = AgentPageLayout.Calculate(new Rect(0f, 0f, 780f, 500f));
        var document = new RimMindUiDocument("agent_active", new Rect(0f, 0f, 780f, 500f), new[]
        {
            RimMindUiElement.Panel("list", layout.List),
            RimMindUiElement.Panel("detail", layout.Detail),
            RimMindUiElement.Label("status", layout.Status, "Nickie - Active"),
            RimMindUiElement.Button("pause", layout.ActionBar.Buttons[0].Rect, "Pause"),
            RimMindUiElement.Panel("activity", layout.Activity),
            RimMindUiElement.Input("chat", layout.Chat, "")
        });

        string html = UiSnapshotHtmlWriter.Write(document);

        Assert.Contains("agent_active", html);
        Assert.Contains("data-name=\"chat\"", html);
        Assert.True(layout.Detail.x > layout.List.xMax);
        Assert.True(layout.Chat.y > layout.Activity.y);
    }
}
