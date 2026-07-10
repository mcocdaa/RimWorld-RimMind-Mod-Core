using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class ContextKeysDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        private readonly RimMindTableDrawer _tableDrawer = new();
        private Vector2 _scrollPosition;

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            var registry = RimMindServiceLocator.TryGet<IContextKeyRegistry>();
            DebugTableModel model = new ContextKeysDebugTableModelBuilder(registry).Build();
            _tableDrawer.Draw(rect, model, ref _scrollPosition, scope);
        }
    }
}
