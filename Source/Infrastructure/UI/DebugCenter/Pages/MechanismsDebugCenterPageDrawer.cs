using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class MechanismsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        private readonly RimMindTableDrawer _tableDrawer = new();
        private Vector2 _scrollPosition;

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            var registry = RimMindServiceLocator.TryGet<IGameMechanismRegistry>();
            DebugTableModel model = new MechanismsDebugTableModelBuilder(registry).Build();
            _tableDrawer.Draw(rect, model, ref _scrollPosition, scope);
        }
    }
}
