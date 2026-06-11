using RimMind.Application.Common.Interfaces.Agent.Perception;
using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Application.Api
{
    public static partial class RimMindAPI
    {
        public static class Perception
        {
            public static IExtensionRegistry<IPerceptionSource> Sources
                => Extensions<IPerceptionSource>();
        }
    }
}
