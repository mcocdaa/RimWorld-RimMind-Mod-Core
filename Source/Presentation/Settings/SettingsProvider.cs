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
        public bool DebugLogging => _settings.debugLogging;
        public int AgentTickInterval => _settings.agentTickInterval;
        public int BehaviorHistoryMax => _settings.behaviorHistoryMax;
    }
}
