using System;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Common;

namespace RimMind.Application.Common.Models.Client
{
    public class TrackedRequest
    {
        public int TrackingId;
        public AIRequest Request = null!;
        public Action<AIResponse> Callback = null!;
        public IAIClient Client = null!;
        public bool IsLocalEndpointSnapshot;
        public AIRequestState State;
        public int EnqueuedAtTick;
        public int StartedProcessingAtTick;
        public int AttemptCount;
        public int MaxAttempts;

        public string RequestId = string.Empty;
        public string ModId = string.Empty;
        public AIRequestPriority Priority;
        public string Status = string.Empty;

        public TrackedRequest() { }

        public TrackedRequest(string requestId, string modId, AIRequestPriority priority, string status)
        {
            RequestId = requestId;
            ModId = modId;
            Priority = priority;
            Status = status;
        }
    }
}
