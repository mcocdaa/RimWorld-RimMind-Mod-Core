namespace RimMind.Application.Common.Models.Client
{
    public enum AIRequestPriority
    {
        Low,
        Normal,
        High,
        Critical,
        Immediate
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
