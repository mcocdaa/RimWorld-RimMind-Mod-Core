using System.Collections.Generic;
using RimMind.Core.UI;

namespace RimMind.Core.Internal
{
    public class OverlayService
    {
        public void RegisterPendingRequest(RequestEntry entry)
            => RequestOverlay.Register(entry);

        public IReadOnlyList<RequestEntry> GetPendingRequests()
            => RequestOverlay.Pending;
    }
}
