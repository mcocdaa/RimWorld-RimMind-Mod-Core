using RimMind.Application.Common.Models.UI;
using RimMind.Infrastructure.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class MechanismsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "mechanisms",
            "RimMind.UI.Hub.Tab.Mechanisms",
            40,
            IsDefault: false);

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            DebugCenterToolGrid.Draw(
                rect,
                ("RimMind.UI.Hub.MechanismStatus", () => Find.WindowStack.Add(new Window_MechanismStatus())));
        }
    }
}
