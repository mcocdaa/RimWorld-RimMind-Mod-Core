using System;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Presentation.Settings;

namespace RimMind.Presentation.Runtime
{
    public class ClientManager : IClientManager
    {
        private IAIClient? _client;
        private Player2Client? _player2Client;
        private AIProvider _lastProvider;
        private string _lastApiKey = "";
        private string _lastEndpoint = "";
        private string _lastModel = "";

        public IAIClient? GetClient()
        {
            var s = RimMindCoreMod.Settings;
            if (s == null) return null;

            if (_client != null &&
                _lastProvider == s.provider &&
                _lastApiKey == s.apiKey &&
                _lastEndpoint == s.apiEndpoint &&
                _lastModel == s.modelName)
                return _client;

            _client = CreateClient(s);
            _lastProvider = s.provider;
            _lastApiKey = s.apiKey;
            _lastEndpoint = s.apiEndpoint;
            _lastModel = s.modelName;
            return _client;
        }

        public Player2Client? GetPlayer2Client()
        {
            var s = RimMindCoreMod.Settings;
            if (s == null) return null;
            if (s.provider != AIProvider.Player2) return null;

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

        private IAIClient? CreateClient(RimMindCoreSettings s)
        {
            return null;
        }
    }
}
