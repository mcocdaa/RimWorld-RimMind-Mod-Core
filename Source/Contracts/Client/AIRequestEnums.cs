using System.Collections.Generic;

namespace RimMind.Core.Client
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
