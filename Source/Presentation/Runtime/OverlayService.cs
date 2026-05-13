using System;
using System.Collections.Generic;
using RimMind.Application.Common.Models.Client;
using Verse;

namespace RimMind.Presentation.Runtime
{
    public class OverlayService
    {
        private readonly List<RequestEntry> _pendingRequests = new List<RequestEntry>();
        private const int MaxEntries = 50;

        public void RegisterPendingRequest(RequestEntry entry)
        {
            if (entry == null) return;
            lock (_pendingRequests)
            {
                _pendingRequests.Add(entry);
                while (_pendingRequests.Count > MaxEntries)
                    _pendingRequests.RemoveAt(0);
            }
        }

        public IReadOnlyList<RequestEntry> GetPendingRequests()
        {
            lock (_pendingRequests)
                return _pendingRequests.AsReadOnly();
        }

        public void Tick()
        {
            int now = Find.TickManager.TicksGame;
            lock (_pendingRequests)
            {
                _pendingRequests.RemoveAll(e => e.ExpireAtTicks > 0 && now > e.ExpireAtTicks);
            }
        }
    }

    public class RequestEntry
    {
        public string RequestId = "";
        public string ModId = "";
        public AIRequestPriority Priority;
        public int EnqueuedTick;
        public int ExpireAtTicks;
        public string Status = "Pending";
    }
}
