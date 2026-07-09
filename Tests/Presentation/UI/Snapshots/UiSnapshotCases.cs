using System.Collections.Generic;
using RimMind.Infrastructure.UI.AgentsPage;
using RimMind.Presentation.UI.Framework;
using UnityEngine;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public static class UiSnapshotCases
{
    public static IReadOnlyList<RimMindUiDocument> All()
    {
        return new[]
        {
            SettingsApi(),
            DebugCenterTabs(),
            DebugOverview(),
            AgentActive(),
            AgentPending(),
            AgentPaused(),
            AgentError(),
            RequestsTable()
        };
    }

    private static RimMindUiDocument SettingsApi()
    {
        var tabs = new[]
        {
            new TabbedPageTabModel("api", "API Config", "RimMind.Settings.Tab.Api", true, true, null),
            new TabbedPageTabModel("queue", "Queue", "RimMind.Settings.Tab.Queue", false, true, null),
            new TabbedPageTabModel("prompts", "Prompts", "RimMind.Settings.Tab.Prompts", false, true, null),
            new TabbedPageTabModel("context", "Context", "RimMind.Settings.Tab.Context", false, true, null)
        };
        TabbedPageLayoutResult layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 780f, 580f), tabs);
        var elements = new List<RimMindUiElement>();
        foreach (TabbedPageTabRect tab in layout.TabRects)
            elements.Add(RimMindUiElement.Tab("tab_" + tab.Id, tab.Rect, tab.Id, tab.Selected));
        elements.Add(RimMindUiElement.Panel("settings_content", layout.Content));
        elements.Add(RimMindUiElement.Label("api_key", new Rect(layout.Content.x + 12f, layout.Content.y + 48f, 300f, 24f), "API Key"));
        elements.Add(RimMindUiElement.Input("api_key_input", new Rect(layout.Content.x + 12f, layout.Content.y + 76f, 520f, 26f), "Saved (35 chars)"));
        return new RimMindUiDocument("settings_api", new Rect(0f, 0f, 780f, 580f), elements);
    }

    private static RimMindUiDocument DebugCenterTabs()
    {
        var tabs = new[]
        {
            new TabbedPageTabModel("overview", "Overview", "RimMind.UI.Hub.Tab.Overview", true, true, null),
            new TabbedPageTabModel("agents", "Agent", "RimMind.UI.Hub.Tab.Agents", false, true, null),
            new TabbedPageTabModel("ai_requests", "AI Requests", "RimMind.UI.Hub.Tab.AIRequests", false, true, null),
            new TabbedPageTabModel("toolcalls", "ToolCall", "RimMind.UI.Hub.Tab.ToolCalls", false, true, null),
            new TabbedPageTabModel("mechanisms", "Mechanism", "RimMind.UI.Hub.Tab.Mechanisms", false, true, null),
            new TabbedPageTabModel("context", "Context", "RimMind.UI.Hub.Tab.ContextKeys", false, true, null)
        };
        TabbedPageLayoutResult layout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 780f, 580f), tabs);
        var elements = new List<RimMindUiElement>();
        foreach (TabbedPageTabRect tab in layout.TabRects)
            elements.Add(RimMindUiElement.Tab("tab_" + tab.Id, tab.Rect, tab.Id, tab.Selected));
        elements.Add(RimMindUiElement.Panel("debug_content", layout.Content));
        return new RimMindUiDocument("debug_center_tabs", new Rect(0f, 0f, 780f, 580f), elements);
    }

    private static RimMindUiDocument DebugOverview()
    {
        var root = new Rect(0f, 0f, 900f, 560f);
        float gap = RimMindUiMetrics.SectionGap;
        float padding = RimMindUiMetrics.Padding;
        float cardW = (root.width - gap) / 2f;
        const float cardH = 104f;
        var health = new Rect(root.x, root.y, cardW, cardH);
        var agents = new Rect(root.x + cardW + gap, root.y, cardW, cardH);
        var queue = new Rect(root.x, root.y + cardH + gap, cardW, cardH);
        var selection = new Rect(root.x + cardW + gap, root.y + cardH + gap, cardW, cardH);

        return new RimMindUiDocument("debug_overview", root, new[]
        {
            RimMindUiElement.Panel("overview_health", health),
            RimMindUiElement.Label("overview_health_title", new Rect(health.x + padding, health.y + padding, health.width - padding * 2f, RimMindUiMetrics.RowHeight), "Health"),
            RimMindUiElement.Label("overview_health_value", new Rect(health.x + padding, health.y + padding + RimMindUiMetrics.RowHeight, health.width - padding * 2f, RimMindUiMetrics.RowHeight), "Operational"),
            RimMindUiElement.Panel("overview_agents", agents),
            RimMindUiElement.Label("overview_agents_title", new Rect(agents.x + padding, agents.y + padding, agents.width - padding * 2f, RimMindUiMetrics.RowHeight), "Agents"),
            RimMindUiElement.Label("overview_agents_value", new Rect(agents.x + padding, agents.y + padding + RimMindUiMetrics.RowHeight, agents.width - padding * 2f, RimMindUiMetrics.RowHeight), "3 / 6"),
            RimMindUiElement.Panel("overview_queue", queue),
            RimMindUiElement.Label("overview_queue_title", new Rect(queue.x + padding, queue.y + padding, queue.width - padding * 2f, RimMindUiMetrics.RowHeight), "Queue"),
            RimMindUiElement.Label("overview_queue_value", new Rect(queue.x + padding, queue.y + padding + RimMindUiMetrics.RowHeight, queue.width - padding * 2f, RimMindUiMetrics.RowHeight), "Queue: Running"),
            RimMindUiElement.Panel("overview_selection", selection),
            RimMindUiElement.Label("overview_selection_title", new Rect(selection.x + padding, selection.y + padding, selection.width - padding * 2f, RimMindUiMetrics.RowHeight), "Selection"),
            RimMindUiElement.Label("overview_selection_value", new Rect(selection.x + padding, selection.y + padding + RimMindUiMetrics.RowHeight, selection.width - padding * 2f, RimMindUiMetrics.RowHeight), "Nickie")
        });
    }

    private static RimMindUiDocument AgentActive()
    {
        AgentPageRects layout = AgentPageLayout.Calculate(new Rect(0f, 0f, 780f, 500f));
        return new RimMindUiDocument("agent_active", new Rect(0f, 0f, 780f, 500f), new[]
        {
            RimMindUiElement.Panel("list", layout.List),
            RimMindUiElement.Panel("detail", layout.Detail),
            RimMindUiElement.Label("status", layout.Status, "Nickie - Active"),
            RimMindUiElement.Button("pause", layout.ActionBar.Buttons[0].Rect, "Pause"),
            RimMindUiElement.Panel("activity", layout.Activity),
            RimMindUiElement.Input("chat", layout.Chat, string.Empty)
        });
    }

    private static RimMindUiDocument AgentPending()
    {
        AgentPageRects layout = AgentPageLayout.Calculate(new Rect(0f, 0f, 780f, 500f));
        return new RimMindUiDocument("agent_pending", new Rect(0f, 0f, 780f, 500f), new[]
        {
            RimMindUiElement.Panel("list", layout.List),
            RimMindUiElement.Panel("detail", layout.Detail),
            RimMindUiElement.Label("status", layout.Status, "Cashton - Pending"),
            RimMindUiElement.Button("create_start", layout.ActionBar.Buttons[0].Rect, "Start"),
            RimMindUiElement.Panel("activity", layout.Activity)
        });
    }

    private static RimMindUiDocument AgentPaused()
    {
        var root = new Rect(0f, 0f, 900f, 560f);
        AgentPageRects layout = AgentPageLayout.Calculate(root);
        return new RimMindUiDocument("agent_paused", root, new[]
        {
            RimMindUiElement.Panel("list", layout.List),
            RimMindUiElement.Panel("activity", layout.Activity),
            RimMindUiElement.Panel("detail", layout.Detail),
            RimMindUiElement.Label("status", layout.Status, "Nickie - Paused"),
            RimMindUiElement.Button("resume", layout.ActionBar.Buttons[0].Rect, "Resume"),
            RimMindUiElement.Input("chat", layout.Chat, string.Empty)
        });
    }

    private static RimMindUiDocument AgentError()
    {
        var root = new Rect(0f, 0f, 900f, 560f);
        AgentPageRects layout = AgentPageLayout.Calculate(root);
        return new RimMindUiDocument("agent_error", root, new[]
        {
            RimMindUiElement.Panel("list", layout.List),
            RimMindUiElement.Panel("activity", layout.Activity),
            RimMindUiElement.Panel("detail", layout.Detail),
            RimMindUiElement.Label("status", layout.Status, "Cashton - Error"),
            RimMindUiElement.Button("open_requests", layout.ActionBar.Buttons[2].Rect, "Open Requests"),
            RimMindUiElement.Input("chat", layout.Chat, string.Empty)
        });
    }

    private static RimMindUiDocument RequestsTable()
    {
        TablePageLayoutResult layout = TablePageLayout.Calculate(new Rect(0f, 0f, 780f, 500f), 20, 4);
        return new RimMindUiDocument("requests_table", new Rect(0f, 0f, 780f, 500f), new[]
        {
            RimMindUiElement.Panel("toolbar", layout.Toolbar),
            RimMindUiElement.TableHeader("header", layout.Header, "Status"),
            RimMindUiElement.Panel("body", layout.Body),
            RimMindUiElement.Panel("bottom", layout.BottomBar)
        });
    }
}
