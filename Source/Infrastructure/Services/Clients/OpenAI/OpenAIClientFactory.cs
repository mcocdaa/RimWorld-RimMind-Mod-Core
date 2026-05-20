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
        public string ProviderId => AIProviders.OpenAI;
        public bool RequiresApiKey => true;

        public IAIClient Create(ISettingsProvider settings)
        {
            if (_openAISettings == null) return null;
            return new OpenAIClient(_openAISettings, _logSink, _aiDebugLog);
        }
    }
}
