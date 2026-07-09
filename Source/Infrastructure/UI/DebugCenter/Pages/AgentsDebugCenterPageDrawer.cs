using RimMind.Infrastructure.UI.AgentsPage;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class AgentsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        private readonly AgentsPageDrawer _drawer = new();

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            _drawer.Draw(rect, context.SelectedPawn, scope);
        }
    }
}
