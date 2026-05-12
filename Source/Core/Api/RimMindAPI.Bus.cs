using RimMind.Adapters.Client.Player2;
using RimMind.Contracts;
using RimMind.Contracts.Client;
using RimMind.Contracts.UI;
using RimMind.Core.Perception;
using RimMind.Core.Runtime;
using System.Collections.Generic;

namespace RimMind.Core
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

            public static Player2Client? GetPlayer2Client()
                => RimMindRuntime.Instance.GetPlayer2Client();
        }
    }
}
