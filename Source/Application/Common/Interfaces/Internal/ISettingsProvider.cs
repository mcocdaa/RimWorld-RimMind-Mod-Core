namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface ISettingsProvider
    {
        int QueueProcessInterval { get; }
        int MaxConcurrentRequests { get; }
        int RequestTimeoutMs { get; }
        int MaxRetryCount { get; }
        int RequestExpireTicks { get; }

        int AgentTickInterval { get; }
        int BehaviorHistoryMax { get; }
        int ThinkCooldownTicks { get; }
        int MaxToolCallDepth { get; }
        int DefaultModCooldownTicks { get; }

        int MaxTokens { get; }
        float DefaultTemperature { get; }
        bool ForceJsonMode { get; }
        string ModelName { get; }

        string Provider { get; }
        string ApiKey { get; }
        string ApiEndpoint { get; }
        string Player2RemoteUrl { get; }

        bool DebugLogging { get; }

        int CircuitBreakerFailureThreshold { get; }
        int CircuitBreakerOpenDurationSec { get; }

        int ContextCalibrateInterval { get; }
        int ContextDiffLifetimeTicks { get; }

        bool IsConfigured { get; }

        IContextSettings Context { get; }

        bool RequestOverlayEnabled { get; set; }
        float RequestOverlayX { get; set; }
        float RequestOverlayY { get; set; }
        float RequestOverlayW { get; set; }
        float RequestOverlayH { get; set; }
    }
}
