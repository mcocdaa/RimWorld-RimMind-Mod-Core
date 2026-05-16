using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Common;

namespace RimMind.Infrastructure.Services.Clients.OpenAI
{
    public class OpenAIClientFactory : IAIClientFactory
    {
        public string Id => AIProviders.OpenAI;
        public string ProviderId => AIProviders.OpenAI;

        public IAIClient Create(ISettingsProvider settings)
        {
            var openAISettings = RimMindServiceLocator.Get<IOpenAISettings>();
            if (openAISettings == null) return null;
            return new OpenAIClient(openAISettings);
        }
    }
}
