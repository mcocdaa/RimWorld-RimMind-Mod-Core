using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class ToolCallsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        private readonly RimMindTableDrawer _tableDrawer = new();
        private Vector2 _scrollPosition;

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            var log = RimMindServiceLocator.TryGet<IAIRequestTraceLog>();
            DebugTableModel model = new ToolCallsDebugTableModelBuilder(log).Build();
            _tableDrawer.Draw(rect, model, ref _scrollPosition, scope);
        }
    }
}
