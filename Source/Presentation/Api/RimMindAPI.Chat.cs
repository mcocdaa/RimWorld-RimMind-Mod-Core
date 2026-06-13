using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Presentation.Runtime;
using Verse;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class ChatFlow
        {
            public static string BuildMapContext(Map map, bool brief = false)
            {
                var builder = RimMindRuntime.Instance.GameContextBuilder;
                return builder.BuildMapContextInstance(map, brief);
            }
        }
    }
}
