using System;
using System.Collections.Generic;
using RimMind.Contracts;
using RimMind.Contracts.Client;

namespace RimMind.Contracts.Internal
{
    public interface IAIRequestQueue
    {
        [ThreadAffinity(ThreadAffinityKind.Any)]
        void Enqueue(AIRequest request, Action<AIResponse> callback, IAIClient client);
        void EnqueueImmediate(AIRequest request, Action<AIResponse> callback, IAIClient client);
        bool CancelRequest(string requestId);
        void CancelAllRequests();
        void PauseQueue();
        void ResumeQueue();
        bool IsPaused { get; }
        int ActiveRequestCount { get; }
        bool IsLocalModelBusy { get; }
        int TotalQueuedCount { get; }
        IReadOnlyList<TrackedRequest> GetActiveRequests();
        IReadOnlyList<TrackedRequest> GetAllQueuedRequests();
        IReadOnlyList<TrackedRequest> GetQueuedRequests(string modId);
        int GetCooldownTicksLeft(string modId);
        int GetQueueDepth(string modId);
        void ClearCooldown(string modId);
        void ClearAllCooldowns();
        void ClearAllQueues();
        IReadOnlyDictionary<string, int> GetAllCooldowns();
        IReadOnlyDictionary<string, int> GetAllQueueDepths();
        void EnqueueLog(string msg, bool isWarning = false);
    }
}
