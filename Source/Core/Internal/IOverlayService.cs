using System.Collections.Generic;
using RimMind.Adapters.UI;

namespace RimMind.Core.Internal
{
    public interface IOverlayService
    {
        void RegisterPendingRequest(RequestEntry entry);
        IReadOnlyList<RequestEntry> GetPendingRequests();
    }
}
