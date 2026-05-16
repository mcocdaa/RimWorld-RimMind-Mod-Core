using RimMind.Application.Common.Interfaces.Internal;

namespace RimMind.Presentation.Settings
{
    internal sealed class SettingsProvider : ISettingsProvider
    {
        private readonly RimMindCoreSettings _settings;

        public SettingsProvider(RimMindCoreSettings settings)
        {
            _settings = settings;
        }

        public int QueueProcessInterval => _settings.queueProcessInterval;
        public int MaxConcurrentRequests => _settings.maxConcurrentRequests;
        public int RequestTimeoutMs => _settings.requestTimeoutMs;
        public int MaxRetryCount => _settings.maxRetryCount;
        public int RequestExpireTicks => _settings.requestExpireTicks;

        public int AgentTickInterval => _settings.agentTickInterval;
        public int BehaviorHistoryMax => _settings.behaviorHistoryMax;
        public int ThinkCooldownTicks => _settings.thinkCooldownTicks;
        public int MaxToolCallDepth => _settings.maxToolCallDepth;
        public int DefaultModCooldownTicks => _settings.defaultModCooldownTicks;

        public int MaxTokens => _settings.maxTokens;
        public float DefaultTemperature => _settings.defaultTemperature;
        public bool ForceJsonMode => _settings.forceJsonMode;
        public string ModelName => _settings.modelName ?? string.Empty;

        public string Provider => _settings.provider ?? string.Empty;
        public string ApiKey => _settings.apiKey ?? string.Empty;
        public string ApiEndpoint => _settings.apiEndpoint ?? string.Empty;
        public string Player2RemoteUrl => _settings.player2RemoteUrl ?? string.Empty;

        public bool DebugLogging => _settings.debugLogging;

        public int CircuitBreakerFailureThreshold => _settings.circuitBreakerFailureThreshold;
        public int CircuitBreakerOpenDurationSec => _settings.circuitBreakerOpenDurationSec;

        public int ContextCalibrateInterval => _settings.contextCalibrateInterval;
        public int ContextDiffLifetimeTicks => _settings.contextDiffLifetimeTicks;

        public bool IsConfigured => _settings.IsConfigured();

        public IContextSettings Context => _settings.Context;

        public bool RequestOverlayEnabled
        {
            get => _settings.requestOverlayEnabled;
            set => _settings.requestOverlayEnabled = value;
        }
        public float RequestOverlayX
        {
            get => _settings.requestOverlayX;
            set => _settings.requestOverlayX = value;
        }
        public float RequestOverlayY
        {
            get => _settings.requestOverlayY;
            set => _settings.requestOverlayY = value;
        }
        public float RequestOverlayW
        {
            get => _settings.requestOverlayW;
            set => _settings.requestOverlayW = value;
        }
        public float RequestOverlayH
        {
            get => _settings.requestOverlayH;
            set => _settings.requestOverlayH = value;
        }
    }
}
