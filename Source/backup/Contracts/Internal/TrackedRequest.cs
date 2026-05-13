using System;
using RimMind.Contracts.Client;

namespace RimMind.Contracts.Internal
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
