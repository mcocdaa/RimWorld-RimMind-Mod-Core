using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public abstract class DebugTablePageBase : DebugCenterPageBase
    {
        private readonly RimMindTableDrawer _tableDrawer = new();
        private Vector2 _scrollPosition;

        public sealed override void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            DebugTableModel model = BuildModel(context);
            _tableDrawer.Draw(rect, model, ref _scrollPosition, scope);
        }

        protected abstract DebugTableModel BuildModel(DebugCenterPageContext context);
    }
}
