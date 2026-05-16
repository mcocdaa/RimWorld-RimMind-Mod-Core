using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.Perception;
using RimMind.Presentation.Runtime;
using System.Collections.Generic;

namespace RimMind.Presentation
{
    public static partial class RimMindAPI
    {
        public static class Bus
        {
            public static IEventBus GetEventBus() => RimMindRuntime.Instance.EventBus;

            public static void PublishPerception(int pawnId, string type, string content, float importance = 0.5f)
                => PerceptionBridge.PublishPerception(pawnId, type, content, importance, GetEventBus());

            public static void RegisterPendingRequest(RequestEntry entry)
                => RimMindRuntime.Instance.OverlayService.RegisterPendingRequest(entry);

            public static IReadOnlyList<RequestEntry> GetPendingRequests()
                => RimMindRuntime.Instance.OverlayService.GetPendingRequests();

            internal static IAIClient? GetClient()
                => RimMindRuntime.Instance.GetClient();

            public static void InvalidateClientCache()
                => RimMindRuntime.Instance.InvalidateClientCache();

            public static IAIClient? GetPlayer2Client()
                => RimMindRuntime.Instance.GetPlayer2Client();
        }
    }
}
