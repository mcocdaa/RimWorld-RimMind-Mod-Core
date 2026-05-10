using System;
using System.Collections.Concurrent;
using RimMind.Contracts;
using RimMind.Contracts.Client;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Settings;
using RimMind.Adapters.Client.OpenAI;
using RimMind.Adapters.Client.Player2;
using RimMind.Kernel.Logging;
using Verse;

namespace RimMind.Core.Runtime
{
    public class ClientManager : IClientManager
    {
        private Player2Client? _cachedPlayer2Client;
        private AIProvider _cachedProvider;
        private readonly object _player2Lock = new object();

        private OpenAIClient? _cachedOpenAIClient;
        private int _lastOpenAISettingsHash;
        private readonly object _openAILock = new object();

        public IAIClient? GetClient()
        {
            var s = RimMindCoreMod.Settings;
            if (!s.IsConfigured()) return null;

            if (s.provider == AIProvider.Player2)
                return EnsurePlayer2Client(s);

            if (s.provider == AIProvider.OpenAI)
            {
                lock (_openAILock)
                {
                    int currentHash = ComputeSettingsHash(s);
                    if (_cachedOpenAIClient != null && currentHash == _lastOpenAISettingsHash)
                        return _cachedOpenAIClient;
                    var client = new OpenAIClient(s);
                    if (client.IsConfigured())
                    {
                        _cachedOpenAIClient = client;
                        _lastOpenAISettingsHash = currentHash;
                        return client;
                    }
                    return null;
                }
            }

            Log.Warning($"[RimMind-Core] Unknown AI provider: {s.provider}, no client available");
            return null;
        }

        public void InvalidateCache()
        {
            lock (_player2Lock)
            {
                if (_cachedProvider == AIProvider.Player2)
                    Player2Client.StopHealthCheck();
                _cachedPlayer2Client = null;
                _cachedProvider = default;
            }
            lock (_openAILock)
            {
                _cachedOpenAIClient = null;
                _lastOpenAISettingsHash = 0;
            }
            OpenAIClient.InvalidateFormatCache();
        }

        public object? GetPlayer2Client()
        {
            lock (_player2Lock)
            {
                return (_cachedPlayer2Client != null && _cachedPlayer2Client.IsConfigured())
                    ? _cachedPlayer2Client : null;
            }
        }

        private Player2Client? EnsurePlayer2Client(RimMindCoreSettings s)
        {
            lock (_player2Lock)
            {
                if (_cachedPlayer2Client != null && _cachedProvider == AIProvider.Player2)
                    return _cachedPlayer2Client.IsConfigured() ? _cachedPlayer2Client : null;
                return null;
            }
        }

        private static int ComputeSettingsHash(RimMindCoreSettings s)
        {
            return System.HashCode.Combine(
                s.apiKey ?? "",
                s.apiEndpoint ?? "",
                s.modelName ?? "",
                s.maxTokens,
                s.defaultTemperature,
                s.forceJsonMode
            );
        }
    }
}
