using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Runtime;
using Verse;

namespace RimMind.Application.Api
{
    public static partial class RimMindAPI
    {
        public static class ChatFlow
        {
            public static string BuildMapContext(Map map, bool brief = false)
            {
                var builder = RimMindServiceLocator.Get<IGameContextBuilder>();
                return builder.BuildMapContextInstance(map, brief);
            }
        }
    }
}
