using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Runtime;
using Verse;
using System;

namespace RimMind.Presentation
{
    public static partial class RimMindAPI
    {
        public static class ChatFlow
        {
            public static string BuildMapContext(Map map, bool brief = false)
            {
                var builder = RimMindServiceLocator.Get<IGameContextBuilder>();
                if (builder is GameContextBuilder gcb)
                    return gcb.BuildMapContextInstance(map, brief);
                return "";
            }
        }
    }
}
