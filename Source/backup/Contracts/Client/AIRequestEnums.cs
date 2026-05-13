using System.Collections.Generic;

namespace RimMind.Contracts.Client
{
    public enum AIRequestPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum AIRequestState
    {
        Queued,
        Processing,
        Completed,
        Error,
        Cancelled
    }
}
