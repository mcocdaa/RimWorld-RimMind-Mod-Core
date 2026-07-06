using RimMind.Application.Common.Models.UI;
using RimMind.Infrastructure.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class ToolCallsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "tool_calls",
            "RimMind.UI.Hub.Tab.ToolCalls",
            30,
            IsDefault: false);

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            DebugCenterToolGrid.Draw(
                rect,
                ("RimMind.UI.Hub.ToolCallDebug", () => Find.WindowStack.Add(new Window_ToolCallDebug())));
        }
    }
}
