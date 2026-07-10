using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public abstract class DebugCenterPageBase : IDebugCenterPageDrawer
    {
        public abstract void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope);

        protected static void DrawUnavailable(Rect rect, string translationKey)
        {
            Color oldColor = GUI.color;
            GUI.color = RimMindUI.ColorMuted;
            Widgets.Label(rect, translationKey.Translate());
            GUI.color = oldColor;
        }
    }
}
