using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class MechanismsDebugCenterPageDrawer : DebugTablePageBase
    {
        protected override DebugTableModel BuildModel(DebugCenterPageContext context)
        {
            var registry = RimMindServiceLocator.TryGet<IGameMechanismRegistry>();
            return new MechanismsDebugTableModelBuilder(registry).Build();
        }
    }
}
