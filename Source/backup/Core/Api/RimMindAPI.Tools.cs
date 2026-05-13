using RimMind.Contracts.Mechanisms;
using RimMind.Contracts.Tools;
using RimMind.Core.Runtime;

namespace RimMind.Core
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
