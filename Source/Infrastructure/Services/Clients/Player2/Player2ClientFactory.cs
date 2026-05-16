using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Common;

namespace RimMind.Infrastructure.Services.Clients.Player2
{
    public class Player2ClientFactory : IAIClientFactory
    {
        public string Id => AIProviders.Player2;
        public string ProviderId => AIProviders.Player2;

        public IAIClient Create(ISettingsProvider settings)
        {
            try
            {
                var client = Player2Client.CreateAsync(settings).GetAwaiter().GetResult();
                return client;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
