using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter
{
    public interface IDebugCenterPageDrawer
    {
        DebugCenterPageDescriptor Descriptor { get; }

        void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope);
    }
}
