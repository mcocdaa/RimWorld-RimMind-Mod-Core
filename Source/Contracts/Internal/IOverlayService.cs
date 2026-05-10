using System.Collections.Generic;

namespace RimMind.Contracts.Internal
{
    public interface IOverlayService
    {
        void RegisterPendingRequest(RimMind.Contracts.UI.RequestEntry entry);
        IReadOnlyList<RimMind.Contracts.UI.RequestEntry> GetPendingRequests();
    }
}
