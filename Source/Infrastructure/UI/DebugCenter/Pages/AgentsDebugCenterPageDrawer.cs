using RimMind.Application.Common.Models.UI;
using RimMind.Infrastructure.UI.AgentsPage;
using RimMind.Infrastructure.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class AgentsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        private readonly AgentsPageDrawer _drawer = new();

        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "agents",
            "RimMind.UI.Hub.Tab.Agents",
            10,
            IsDefault: false);

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            _drawer.Draw(rect, context.SelectedPawn, scope);
        }
    }
}
