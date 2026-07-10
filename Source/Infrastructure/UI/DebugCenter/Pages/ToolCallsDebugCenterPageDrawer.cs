using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class ToolCallsDebugCenterPageDrawer : DebugTablePageBase
    {
        protected override DebugTableModel BuildModel(DebugCenterPageContext context)
        {
            var log = RimMindServiceLocator.TryGet<IAIRequestTraceLog>();
            return new ToolCallsDebugTableModelBuilder(log).Build();
        }
    }
}
