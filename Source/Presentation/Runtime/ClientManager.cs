using System;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Presentation.Settings;

namespace RimMind.Presentation.Runtime
{
    public class ClientManager
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
            _player2Client = new Player2Client(s.player2RemoteUrl);
            return _player2Client;
        }

        public void Invalidate()
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
