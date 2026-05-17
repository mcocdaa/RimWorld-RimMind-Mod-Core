using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Domain.Common;
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

        public static IStorageDriver GetDriver()
        {
            lock (_lock)
            {
                var s = RimMindServiceLocator.Get<IApiCredentialSettings>();
                var historyManager = RimMindServiceLocator.Get<IHistoryManager>();
                if (s == null) return new LocalStorageDriver(historyManager);

                if (_cachedDriver != null && _cachedProvider == s.Provider)
                    return _cachedDriver;

                _cachedProvider = s.Provider;

                if (s.Provider == AIProviders.Player2)
                {
                    var client = RimMindServiceLocator.Get<IClientManager>()?.GetPlayer2Client() as Player2Client;
                    if (client != null && client.IsConfigured())
                    {
                        _cachedDriver = new HybridStorageDriver(client, historyManager);
                        return _cachedDriver;
                    }
                    RimMindErrors.Warn("[RimMind-Core] Player2 client not available, falling back to LocalStorageDriver");
                }

                _cachedDriver = new LocalStorageDriver(historyManager);
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
}
