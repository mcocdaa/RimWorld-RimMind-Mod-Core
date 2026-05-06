using System;
using RimMind.Core.Client;

namespace RimMind.Core.Internal
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
