using System.Collections.Generic;
using RimMind.Application.Common.Models.UI;

namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IOverlayService
    {
        void RegisterPendingRequest(RequestEntry entry);
        IReadOnlyList<RequestEntry> GetPendingRequests();
    }
}
