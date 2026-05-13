using RimMind.Contracts.UI;
using RimMind.Contracts.Internal;
using System.Collections.Generic;
using RimMind.Adapters.UI;

namespace RimMind.Core.Runtime
{
    public class OverlayService : IOverlayService
    {
        public void RegisterPendingRequest(RequestEntry entry)
            => RequestOverlay.Register(entry);

        public IReadOnlyList<RequestEntry> GetPendingRequests()
            => RequestOverlay.Pending;
    }
}
