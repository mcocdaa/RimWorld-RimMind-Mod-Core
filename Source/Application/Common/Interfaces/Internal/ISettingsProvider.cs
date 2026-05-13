namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface ISettingsProvider
    {
        int QueueProcessInterval { get; }
        int MaxConcurrentRequests { get; }
        int RequestTimeoutMs { get; }
        bool DebugLogging { get; }
        int AgentTickInterval { get; }
        int BehaviorHistoryMax { get; }
    }
}
