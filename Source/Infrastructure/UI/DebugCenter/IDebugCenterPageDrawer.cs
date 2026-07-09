using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter
{
    public interface IDebugCenterPageDrawer
    {
        void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope);
    }
}
