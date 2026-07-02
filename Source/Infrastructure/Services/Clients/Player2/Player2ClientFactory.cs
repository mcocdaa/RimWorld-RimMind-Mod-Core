using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Common;

namespace RimMind.Infrastructure.Services.Clients.Player2
{
    public class Player2ClientFactory : IAIClientFactory
    {
        private readonly ILogSink? _logSink;
        private readonly IAIDebugLog? _aiDebugLog;

        public Player2ClientFactory(ILogSink? logSink = null, IAIDebugLog? aiDebugLog = null)
        {
            _logSink = logSink;
            _aiDebugLog = aiDebugLog;
        }

        public string Id => AIProviders.Player2;
        public string OwnerModId => RimMindOwnerConsts.CoreModId;
        public string ProviderId => AIProviders.Player2;
        public bool RequiresApiKey => false;

        public IAIClient Create(ISettingsProvider settings)
        {
            try
            {
                var client = Player2Client.CreateAsync(settings, _logSink, _aiDebugLog).GetAwaiter().GetResult();
                return client;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
