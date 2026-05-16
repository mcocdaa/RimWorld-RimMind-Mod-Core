using RimMind.Domain.Enums;

namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface ISettingsProvider
    {
        int QueueProcessInterval { get; set; }
        int MaxConcurrentRequests { get; set; }
        int RequestTimeoutMs { get; set; }
        int MaxRetryCount { get; set; }
        int RequestExpireTicks { get; set; }

        int AgentTickInterval { get; }
        int BehaviorHistoryMax { get; set; }
        int ThinkCooldownTicks { get; }
        int MaxToolCallDepth { get; }
        int DefaultModCooldownTicks { get; set; }

        int MaxTokens { get; set; }
        float DefaultTemperature { get; set; }
        bool ForceJsonMode { get; set; }
        string ModelName { get; set; }

        string Provider { get; set; }
        string ApiKey { get; set; }
        string ApiEndpoint { get; set; }
        string Player2RemoteUrl { get; set; }

        bool DebugLogging { get; set; }

        int CircuitBreakerFailureThreshold { get; }
        int CircuitBreakerOpenDurationSec { get; }

        int ContextCalibrateInterval { get; set; }
        int ContextDiffLifetimeTicks { get; set; }

        bool IsConfigured { get; }

        IContextSettings Context { get; }

        bool RequestOverlayEnabled { get; set; }
        float RequestOverlayX { get; set; }
        float RequestOverlayY { get; set; }
        float RequestOverlayW { get; set; }
        float RequestOverlayH { get; set; }

        string CustomPawnPrompt { get; set; }
        string CustomMapPrompt { get; set; }

        FlywheelAutoApplyMode AutoApplyMode { get; set; }
        float AutoApplyConfidenceThreshold { get; set; }

        bool IsOpenAIConfigured();
    }
}
