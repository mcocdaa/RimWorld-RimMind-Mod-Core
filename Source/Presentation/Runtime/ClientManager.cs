using System;
using System.Linq;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Common;
using RimMind.Infrastructure.Services.Clients.Player2;

namespace RimMind.Presentation.Runtime
{
    public class ClientManager : IClientManager
    {
        private IAIClient? _client;
        private Player2Client? _player2Client;
        private string _lastProvider = "";
        private string _lastApiKey = "";
        private string _lastEndpoint = "";
        private string _lastModel = "";

        public IAIClient? GetClient()
        {
            var s = RimMindServiceLocator.Get<ISettingsProvider>();
            if (s == null) return null;

            if (_client != null &&
                _lastProvider == s.Provider &&
                _lastApiKey == s.ApiKey &&
                _lastEndpoint == s.ApiEndpoint &&
                _lastModel == s.ModelName)
                return _client;

            _client = CreateClient(s);
            _lastProvider = s.Provider;
            _lastApiKey = s.ApiKey;
            _lastEndpoint = s.ApiEndpoint;
            _lastModel = s.ModelName;
            return _client;
        }

        public Player2Client? GetPlayer2Client()
        {
            var s = RimMindServiceLocator.Get<ISettingsProvider>();
            if (s == null) return null;
            if (s.Provider != AIProviders.Player2) return null;

            if (_player2Client != null) return _player2Client;
            try
            {
                _player2Client = Player2Client.CreateAsync(s).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                RimMind.Application.Features.Queue.AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Failed to create Player2 client: {ex.Message}", isWarning: true);
            }
            return _player2Client;
        }

        object? IClientManager.GetPlayer2Client() => GetPlayer2Client();

        public void InvalidateCache()
        {
            _client = null;
            _player2Client = null;
        }

        private IAIClient? CreateClient(ISettingsProvider s)
        {
            var factoryRegistry = RimMindServiceLocator.Get<IExtensionRegistry<IAIClientFactory>>();
            var factory = factoryRegistry?.All.FirstOrDefault(f => f.ProviderId == s.Provider);
            return factory?.Create(s);
        }
    }
}
