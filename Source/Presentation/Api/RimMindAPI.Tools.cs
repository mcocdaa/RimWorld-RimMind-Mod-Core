using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class ToolSet
        {
            public static IToolRegistry Registry => RimMindRuntime.Instance.ToolRegistry;
            public static IGameMechanismRegistry Mechanisms => RimMindRuntime.Instance.MechanismRegistry;
        }
    }
}
