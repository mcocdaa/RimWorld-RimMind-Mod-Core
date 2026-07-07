using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class ContextKeysDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "context_keys",
            "RimMind.UI.Hub.Tab.ContextKeys",
            50,
            IsDefault: false);

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            DebugCenterToolGrid.Draw(
                rect,
                scope,
                ("RimMind.UI.Hub.ContextKeys", () => Find.WindowStack.Add(new Window_ContextKeyDebug())));
        }
    }
}
