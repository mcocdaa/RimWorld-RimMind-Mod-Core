using RimMind.Application.Common.Models.UI;
using RimMind.Infrastructure.UI.AIRequestsPage;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class AIRequestsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        private readonly AIRequestsPageDrawer _drawer = new();

        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "ai_requests",
            "RimMind.UI.Hub.Tab.AIRequests",
            20,
            IsDefault: true);

        public void Draw(Rect rect, DebugCenterPageContext context)
        {
            _drawer.Draw(rect);
        }
    }
}
