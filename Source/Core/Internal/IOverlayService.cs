using System.Collections.Generic;

namespace RimMind.Core.Internal
{
    public interface IOverlayService
    {
        void RegisterPendingRequest(RimMind.Contracts.UI.RequestEntry entry);
        IReadOnlyList<RimMind.Contracts.UI.RequestEntry> GetPendingRequests();
    }
}
