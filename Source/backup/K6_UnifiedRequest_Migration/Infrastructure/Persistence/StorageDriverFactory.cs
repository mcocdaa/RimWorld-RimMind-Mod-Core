using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Helpers;
using RimMind.Application.Common.Models.Npc;
using RimMind.Domain.ValueObjects;

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
        private static StorageDriverDependencies? _deps;

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
            IContextBuilder? contextEngine,
            IGameContextBuilder? gameContextBuilder,
            IResponseDispatcher? responseDispatcher)
        {
            _apiCredentialSettings = apiCredentialSettings;
            _historyManager = historyManager;
            _clientManager = clientManager;
            _deps = (npcManager != null && logSink != null && contextEngine != null
                && settingsProvider != null && gameContextBuilder != null && responseDispatcher != null)
                ? new StorageDriverDependencies(npcManager, logSink, contextEngine,
                    settingsProvider, gameContextBuilder, responseDispatcher)
                : null;
        }

        public static IStorageDriver GetDriver()
        {
            lock (_lock)
            {
                var s = _apiCredentialSettings;
                var historyManager = _historyManager;
                if (s == null || historyManager == null) return new LocalStorageDriver(historyManager!, settingsProvider: _deps?.SettingsProvider, clientManager: _clientManager, contextEngine: _deps?.ContextBuilder);

                if (_cachedDriver != null && _cachedProvider == s.Provider)
                    return _cachedDriver;

                _cachedProvider = s.Provider;

                if (!AIProviderRegistry.RequiresApiKey(s.Provider))
                {
                    var hybridDriver = _clientManager?.TryCreateHybridStorageDriver(historyManager, _deps!);
                    if (hybridDriver != null)
                    {
                        _cachedDriver = hybridDriver;
                        return _cachedDriver;
                    }
                    RimMindErrors.Warn("[RimMind-Core] Player2 client not available, falling back to LocalStorageDriver");
                }

                _cachedDriver = new LocalStorageDriver(historyManager, settingsProvider: _deps?.SettingsProvider, clientManager: _clientManager, contextEngine: _deps?.ContextBuilder);
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
