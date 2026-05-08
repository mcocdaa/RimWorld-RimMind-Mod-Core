using RimMind.Contracts.UI;
using System.Collections.Generic;
using RimMind.Adapters.UI;

namespace RimMind.Core.Internal
{
    public class OverlayService : IOverlayService
    {
        public void RegisterPendingRequest(RequestEntry entry)
            => RequestOverlay.Register(entry);

        public IReadOnlyList<RequestEntry> GetPendingRequests()
            => RequestOverlay.Pending;
    }
}
