using RimMind.Application.Common.Models.UI;
using RimMind.Infrastructure.UI.DebugCenter;
using RimMind.Infrastructure.UI.DebugCenter.Pages;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages;

internal sealed class OverviewDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
    public OverviewDebugCenterPageDrawer()
        : base("overview", "RimMind.UI.Hub.Tab.Overview", 0, false)
    {
    }
}

internal sealed class AgentsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
    public AgentsDebugCenterPageDrawer()
        : base("agents", "RimMind.UI.Hub.Tab.Agents", 10, false)
    {
    }
}

internal sealed class AIRequestsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
    public AIRequestsDebugCenterPageDrawer()
        : base("ai_requests", "RimMind.UI.Hub.Tab.AIRequests", 20, true)
    {
    }
}

internal sealed class ToolCallsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
    public ToolCallsDebugCenterPageDrawer()
        : base("tool_calls", "RimMind.UI.Hub.Tab.ToolCalls", 30, false)
    {
    }
}

internal sealed class MechanismsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
    public MechanismsDebugCenterPageDrawer()
        : base("mechanisms", "RimMind.UI.Hub.Tab.Mechanisms", 40, false)
    {
    }
}

internal sealed class ContextKeysDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
    public ContextKeysDebugCenterPageDrawer()
        : base("context_keys", "RimMind.UI.Hub.Tab.ContextKeys", 50, false)
    {
    }
}

internal sealed class SettingsEntryDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
    public SettingsEntryDebugCenterPageDrawer()
        : base("settings", "RimMind.UI.Hub.Tab.Settings", 60, false)
    {
    }
}

internal abstract class TestDebugCenterPageDrawer : IDebugCenterPageDrawer
{
    protected TestDebugCenterPageDrawer(string id, string labelKey, int order, bool isDefault)
    {
        Descriptor = new DebugCenterPageDescriptor(id, labelKey, order, isDefault);
    }

    public DebugCenterPageDescriptor Descriptor { get; }

    public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
    {
    }
}
