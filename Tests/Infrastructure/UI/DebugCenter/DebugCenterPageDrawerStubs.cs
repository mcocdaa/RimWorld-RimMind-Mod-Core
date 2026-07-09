using RimMind.Infrastructure.UI.DebugCenter;
using RimMind.Infrastructure.UI.DebugCenter.Pages;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages;

internal sealed class OverviewDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
}

internal sealed class AgentsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
}

internal sealed class AIRequestsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
}

internal sealed class ToolCallsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
}

internal sealed class MechanismsDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
}

internal sealed class ContextKeysDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
}

internal sealed class SettingsEntryDebugCenterPageDrawer : TestDebugCenterPageDrawer
{
}

internal abstract class TestDebugCenterPageDrawer : IDebugCenterPageDrawer
{
    public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
    {
    }
}
