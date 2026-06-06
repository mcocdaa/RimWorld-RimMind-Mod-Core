using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Common;

namespace RimMind.Infrastructure.Services.Clients.OpenAI
{
    public class OpenAIClientFactory : IAIClientFactory
    {
        private readonly IOpenAISettings? _openAISettings;
        private readonly ILogSink? _logSink;
        private readonly IAIDebugLog? _aiDebugLog;

        public OpenAIClientFactory(IOpenAISettings? openAISettings = null, ILogSink? logSink = null, IAIDebugLog? aiDebugLog = null)
        {
            _openAISettings = openAISettings;
            _logSink = logSink;
            _aiDebugLog = aiDebugLog;
        }

        public string Id => AIProviders.OpenAI;
        public string OwnerModId => "RimMindCore";
        public string ProviderId => AIProviders.OpenAI;
        public bool RequiresApiKey => true;

        public IAIClient Create(ISettingsProvider settings)
        {
            var resolvedSettings = settings != null
                ? new OpenAISettingsAdapter(settings)
                : _openAISettings;
            if (resolvedSettings == null) return null;
            return new OpenAIClient(resolvedSettings, _logSink, _aiDebugLog);
        }

        private sealed class OpenAISettingsAdapter : IOpenAISettings
        {
            private readonly ISettingsProvider _settings;

            public OpenAISettingsAdapter(ISettingsProvider settings)
            {
                _settings = settings;
            }

            public string ApiEndpoint => _settings.ApiEndpoint;
            public string ModelName => _settings.ModelName;
            public string ApiKey => _settings.ApiKey;
            public bool ForceJsonMode => _settings.ForceJsonMode;
            public int MaxTokens => _settings.MaxTokens;
            public float DefaultTemperature => _settings.DefaultTemperature;
            public bool DebugLogging => _settings.DebugLogging;
            public bool IsConfigured() => _settings.IsOpenAIConfigured();
        }
    }
}
