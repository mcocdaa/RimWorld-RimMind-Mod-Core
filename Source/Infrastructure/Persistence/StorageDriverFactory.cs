using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Helpers;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Services.Clients.Player2;
using Verse;

namespace RimMind.Infrastructure.Persistence
{
    public static class StorageDriverFactory
    {
        private static readonly object _lock = new object();
        private static IStorageDriver? _cachedDriver;
        private static string? _cachedProvider;

        // Injected dependencies — set by Initialize()
        private static IApiCredentialSettings? _apiCredentialSettings;
        private static IHistoryManager? _historyManager;
        private static IClientManager? _clientManager;
        private static INpcManager? _npcManager;
        private static ILogSink? _logSink;
        private static ISettingsProvider? _settingsProvider;
        private static IContextEngine? _contextEngine;
        private static IGameContextBuilder? _gameContextBuilder;
        private static IResponseDispatcher? _responseDispatcher;

        /// <summary>
        /// Inject all required dependencies. Called from RimMindRuntime after services are registered.
        /// </summary>
        public static void Initialize(
            IApiCredentialSettings? apiCredentialSettings,
            IHistoryManager? historyManager,
            IClientManager? clientManager,
            INpcManager? npcManager,
            ILogSink? logSink,
            ISettingsProvider? settingsProvider,
            IContextEngine? contextEngine,
            IGameContextBuilder? gameContextBuilder,
            IResponseDispatcher? responseDispatcher)
        {
            _apiCredentialSettings = apiCredentialSettings;
            _historyManager = historyManager;
            _clientManager = clientManager;
            _npcManager = npcManager;
            _logSink = logSink;
            _settingsProvider = settingsProvider;
            _contextEngine = contextEngine;
            _gameContextBuilder = gameContextBuilder;
            _responseDispatcher = responseDispatcher;
        }

        public static IStorageDriver GetDriver()
        {
            lock (_lock)
            {
                var s = _apiCredentialSettings;
                var historyManager = _historyManager;
                if (s == null || historyManager == null) return new LocalStorageDriver(historyManager!, settingsProvider: _settingsProvider, clientManager: _clientManager, contextEngine: _contextEngine);

                if (_cachedDriver != null && _cachedProvider == s.Provider)
                    return _cachedDriver;

                _cachedProvider = s.Provider;

                if (!ProviderHelper.RequiresApiKey(s.Provider))
                {
                    var client = _clientManager?.GetPlayer2Client() as Player2Client;
                    if (client != null && client.IsConfigured())
                    {
                        _cachedDriver = new HybridStorageDriver(client, historyManager,
                            _npcManager!, logSink: _logSink, settingsProvider: _settingsProvider,
                            clientManager: _clientManager, contextEngine: _contextEngine,
                            gameContextBuilder: _gameContextBuilder, responseDispatcher: _responseDispatcher);
                        return _cachedDriver;
                    }
                    RimMindErrors.Warn("[RimMind-Core] Player2 client not available, falling back to LocalStorageDriver");
                }

                _cachedDriver = new LocalStorageDriver(historyManager, settingsProvider: _settingsProvider, clientManager: _clientManager, contextEngine: _contextEngine);
                return _cachedDriver;
            }
        }

        public static void InvalidateCache()
        {
            lock (_lock)
            {
                _cachedDriver = null;
                _cachedProvider = null;
            }
        }
    }

    internal sealed class StorageDriverFactoryService : IStorageDriverFactory
    {
        public IStorageDriver? GetDriver()
        {
            try { return StorageDriverFactory.GetDriver(); }
            catch (Exception ex) { RimMindErrors.Warn($"Failed to create storage driver: {ex}"); return null; }
        }
    }
}
