using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Enums;

namespace RimMind.Presentation.Settings
{
    internal sealed class SettingsProvider : ISettingsProvider
    {
        private readonly RimMindCoreSettings _settings;

        public SettingsProvider(RimMindCoreSettings settings)
        {
            _settings = settings;
        }

        public int QueueProcessInterval
        {
            get => _settings.queueProcessInterval;
            set => _settings.queueProcessInterval = value;
        }
        public int MaxConcurrentRequests
        {
            get => _settings.maxConcurrentRequests;
            set => _settings.maxConcurrentRequests = value;
        }
        public int RequestTimeoutMs
        {
            get => _settings.requestTimeoutMs;
            set => _settings.requestTimeoutMs = value;
        }
        public int MaxRetryCount
        {
            get => _settings.maxRetryCount;
            set => _settings.maxRetryCount = value;
        }
        public int RequestExpireTicks
        {
            get => _settings.requestExpireTicks;
            set => _settings.requestExpireTicks = value;
        }

        public int AgentTickInterval => _settings.agentTickInterval;
        public int BehaviorHistoryMax
        {
            get => _settings.behaviorHistoryMax;
            set => _settings.behaviorHistoryMax = value;
        }
        public int ThinkCooldownTicks => _settings.thinkCooldownTicks;
        public int MaxToolCallDepth => _settings.maxToolCallDepth;
        public int DefaultModCooldownTicks
        {
            get => _settings.defaultModCooldownTicks;
            set => _settings.defaultModCooldownTicks = value;
        }

        public int MaxTokens
        {
            get => _settings.maxTokens;
            set => _settings.maxTokens = value;
        }
        public float DefaultTemperature
        {
            get => _settings.defaultTemperature;
            set => _settings.defaultTemperature = value;
        }
        public bool ForceJsonMode
        {
            get => _settings.forceJsonMode;
            set => _settings.forceJsonMode = value;
        }
        public string ModelName
        {
            get => _settings.modelName ?? string.Empty;
            set => _settings.modelName = value;
        }

        public string Provider
        {
            get => _settings.provider ?? string.Empty;
            set => _settings.provider = value;
        }
        public string ApiKey
        {
            get => _settings.apiKey ?? string.Empty;
            set => _settings.apiKey = value;
        }
        public string ApiEndpoint
        {
            get => _settings.apiEndpoint ?? string.Empty;
            set => _settings.apiEndpoint = value;
        }
        public string Player2RemoteUrl
        {
            get => _settings.player2RemoteUrl ?? string.Empty;
            set => _settings.player2RemoteUrl = value;
        }

        public bool DebugLogging
        {
            get => _settings.debugLogging;
            set => _settings.debugLogging = value;
        }

        public int CircuitBreakerFailureThreshold => _settings.circuitBreakerFailureThreshold;
        public int CircuitBreakerOpenDurationSec => _settings.circuitBreakerOpenDurationSec;

        public int ContextCalibrateInterval
        {
            get => _settings.contextCalibrateInterval;
            set => _settings.contextCalibrateInterval = value;
        }
        public int ContextDiffLifetimeTicks
        {
            get => _settings.contextDiffLifetimeTicks;
            set => _settings.contextDiffLifetimeTicks = value;
        }

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

        public string CustomPawnPrompt
        {
            get => _settings.customPawnPrompt ?? string.Empty;
            set => _settings.customPawnPrompt = value;
        }
        public string CustomMapPrompt
        {
            get => _settings.customMapPrompt ?? string.Empty;
            set => _settings.customMapPrompt = value;
        }

        public FlywheelAutoApplyMode AutoApplyMode
        {
            get => _settings.autoApplyMode;
            set => _settings.autoApplyMode = value;
        }
        public float AutoApplyConfidenceThreshold
        {
            get => _settings.autoApplyConfidenceThreshold;
            set => _settings.autoApplyConfidenceThreshold = value;
        }

        public bool IsOpenAIConfigured() => _settings.IsOpenAIConfigured();

        bool IContextIncludeSettings.IncludeRace { get => Context.IncludeRace; set => Context.IncludeRace = value; }
        bool IContextIncludeSettings.IncludeAge { get => Context.IncludeAge; set => Context.IncludeAge = value; }
        bool IContextIncludeSettings.IncludeGender { get => Context.IncludeGender; set => Context.IncludeGender = value; }
        bool IContextIncludeSettings.IncludeBackstory { get => Context.IncludeBackstory; set => Context.IncludeBackstory = value; }
        bool IContextIncludeSettings.IncludeIdeology { get => Context.IncludeIdeology; set => Context.IncludeIdeology = value; }
        bool IContextIncludeSettings.IncludeTraits { get => Context.IncludeTraits; set => Context.IncludeTraits = value; }
        bool IContextIncludeSettings.IncludeSkills { get => Context.IncludeSkills; set => Context.IncludeSkills = value; }
        int IContextIncludeSettings.MinSkillLevel { get => Context.MinSkillLevel; set => Context.MinSkillLevel = value; }
        bool IContextIncludeSettings.IncludeHealth { get => Context.IncludeHealth; set => Context.IncludeHealth = value; }
        bool IContextIncludeSettings.IncludeCapacities { get => Context.IncludeCapacities; set => Context.IncludeCapacities = value; }
        bool IContextIncludeSettings.IncludeMood { get => Context.IncludeMood; set => Context.IncludeMood = value; }
        bool IContextIncludeSettings.IncludeMoodThoughts { get => Context.IncludeMoodThoughts; set => Context.IncludeMoodThoughts = value; }
        bool IContextIncludeSettings.IncludeCurrentJob { get => Context.IncludeCurrentJob; set => Context.IncludeCurrentJob = value; }
        bool IContextIncludeSettings.IncludeWorkPriorities { get => Context.IncludeWorkPriorities; set => Context.IncludeWorkPriorities = value; }
        bool IContextIncludeSettings.IncludeEquipment { get => Context.IncludeEquipment; set => Context.IncludeEquipment = value; }
        bool IContextIncludeSettings.IncludeInventory { get => Context.IncludeInventory; set => Context.IncludeInventory = value; }
        bool IContextIncludeSettings.IncludeLocation { get => Context.IncludeLocation; set => Context.IncludeLocation = value; }
        bool IContextIncludeSettings.IncludeRelations { get => Context.IncludeRelations; set => Context.IncludeRelations = value; }
        bool IContextIncludeSettings.IncludeGenes { get => Context.IncludeGenes; set => Context.IncludeGenes = value; }
        bool IContextIncludeSettings.IncludeSurroundings { get => Context.IncludeSurroundings; set => Context.IncludeSurroundings = value; }
        bool IContextIncludeSettings.IncludeCombatStatus { get => Context.IncludeCombatStatus; set => Context.IncludeCombatStatus = value; }
        bool IContextIncludeSettings.IncludeGameTime { get => Context.IncludeGameTime; set => Context.IncludeGameTime = value; }
        bool IContextIncludeSettings.IncludeColonistCount { get => Context.IncludeColonistCount; set => Context.IncludeColonistCount = value; }
        bool IContextIncludeSettings.IncludeColonistNames { get => Context.IncludeColonistNames; set => Context.IncludeColonistNames = value; }
        bool IContextIncludeSettings.IncludeWealth { get => Context.IncludeWealth; set => Context.IncludeWealth = value; }
        bool IContextIncludeSettings.IncludeFood { get => Context.IncludeFood; set => Context.IncludeFood = value; }
        bool IContextIncludeSettings.IncludeSeason { get => Context.IncludeSeason; set => Context.IncludeSeason = value; }
        bool IContextIncludeSettings.IncludeWeather { get => Context.IncludeWeather; set => Context.IncludeWeather = value; }
        bool IContextIncludeSettings.IncludeThreats { get => Context.IncludeThreats; set => Context.IncludeThreats = value; }

        float IContextBudgetSettings.ContextBudget { get => Context.ContextBudget; set => Context.ContextBudget = value; }
        int IContextBudgetSettings.ContextBriefLimit => Context.ContextBriefLimit;
        int IContextBudgetSettings.MaxCacheEntries => Context.MaxCacheEntries;
        float IContextBudgetSettings.BudgetW1 { get => Context.BudgetW1; set => Context.BudgetW1 = value; }
        float IContextBudgetSettings.BudgetW2 { get => Context.BudgetW2; set => Context.BudgetW2 = value; }

        int IContextEnvironmentSettings.EnvironmentScanRadius => Context.EnvironmentScanRadius;
        int IContextEnvironmentSettings.EnvironmentMaxItems => Context.EnvironmentMaxItems;
        float IContextEnvironmentSettings.ThreatThresholdHigh => Context.ThreatThresholdHigh;
        float IContextEnvironmentSettings.ThreatThresholdMedium => Context.ThreatThresholdMedium;
        float IContextEnvironmentSettings.ThreatThresholdLow => Context.ThreatThresholdLow;
        float IContextEnvironmentSettings.MoodDiffThreshold => Context.MoodDiffThreshold;
        float IContextEnvironmentSettings.TemperatureDiffThreshold => Context.TemperatureDiffThreshold;

        void IContextSettings.ApplyPreset(ContextPreset preset) => Context.ApplyPreset(preset);
        void IContextSettings.ResetToDefault() => Context.ResetToDefault();
    }
}
