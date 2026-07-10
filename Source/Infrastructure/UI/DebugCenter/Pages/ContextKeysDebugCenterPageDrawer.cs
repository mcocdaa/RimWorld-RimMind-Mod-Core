using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class ContextKeysDebugCenterPageDrawer : DebugTablePageBase
    {
        protected override DebugTableModel BuildModel(DebugCenterPageContext context)
        {
            var registry = RimMindServiceLocator.TryGet<IContextKeyRegistry>();
            return new ContextKeysDebugTableModelBuilder(registry).Build();
        }
    }
}
