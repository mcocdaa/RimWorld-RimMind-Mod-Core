using System;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;

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
    }
}
