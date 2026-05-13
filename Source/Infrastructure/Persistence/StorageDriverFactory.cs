using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Presentation;
using Verse;

namespace RimMind.Infrastructure.Persistence
{
    public static class StorageDriverFactory
    {
        private static readonly object _lock = new object();
        private static IStorageDriver? _cachedDriver;
        private static AIProvider _cachedProvider;

        public static IStorageDriver GetDriver()
        {
            lock (_lock)
            {
                var s = RimMindCoreMod.Settings;
                var historyManager = RimMindAPI.GetHistoryManager();
                if (s == null) return new LocalStorageDriver(historyManager);

                if (_cachedDriver != null && _cachedProvider == s.provider)
                    return _cachedDriver;

                _cachedProvider = s.provider;

                if (s.provider == AIProvider.Player2)
                {
                    var client = RimMindAPI.GetPlayer2Client();
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
                _cachedProvider = default;
            }
        }
    }
}
