namespace RimMind.Application.Common.Models
{
    /// <summary>
    /// Shared default values used across RimMind-Core.
    /// Centralizes magic numbers to ensure consistency and single source of truth.
    /// </summary>
    public static class RimMindDefaults
    {
        /// <summary>Default maximum tokens for AI requests.</summary>
        public const int MaxTokens = 800;

        /// <summary>Default request expiration time in game ticks.</summary>
        public const int RequestExpireTicks = 30000;

        /// <summary>Default token limit for test connection requests.</summary>
        public const int TestConnectionMaxTokens = 60;

        /// <summary>Default number of history records to retrieve.</summary>
        public const int DefaultHistoryLimit = 20;

        /// <summary>Default number of telemetry records to retrieve.</summary>
        public const int TelemetryRecordLimit = 100;

        /// <summary>Default string preview truncation length.</summary>
        public const int PreviewTruncateLength = 120;

        /// <summary>Default description truncation length.</summary>
        public const int DescriptionTruncateLength = 80;

        /// <summary>Default letter choice expiry in game ticks (≈60 seconds at 60 tps).</summary>
        public const int LetterChoiceExpireTicks = 3600;

        /// <summary>Default TraceId length (substring of GUID).</summary>
        public const int TraceIdLength = 8;

        /// <summary>Default Agent tick interval in game ticks.</summary>
        public const int AgentTickInterval = 150;

        /// <summary>Default behavior history maximum entries.</summary>
        public const int BehaviorHistoryMax = 100;

        /// <summary>Default queue processing interval in game ticks.</summary>
        public const int QueueProcessInterval = 60;

        /// <summary>Default flywheel calibration interval in game ticks.</summary>
        public const int FlywheelCalibrateInterval = 10000;

        /// <summary>Default storage driver history query limit.</summary>
        public const int StorageHistoryLimit = 50;

        /// <summary>Default storage driver memory query limit.</summary>
        public const int StorageMemoryQueryLimit = 10;
    }
}
