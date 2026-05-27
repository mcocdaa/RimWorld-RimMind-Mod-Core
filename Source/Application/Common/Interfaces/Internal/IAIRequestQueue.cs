using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IAIRequestQueue
    {
        [ThreadAffinity(ThreadAffinityKind.Any)]
        void Enqueue(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> callback, IAIClient client);
        void EnqueueImmediate(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> callback, IAIClient client);
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
