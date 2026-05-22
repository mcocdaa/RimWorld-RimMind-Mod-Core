using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Npc;

namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IClientManager
    {
        IAIClient? GetClient();
        void InvalidateCache();
        IAIClient? GetPlayer2Client();

        /// <summary>
        /// Attempts to create a HybridStorageDriver if a configured Player2 client is available.
        /// Returns null if Player2 is not available or not configured.
        /// Encapsulates the Player2Client type knowledge within the implementation.
        /// </summary>
        IStorageDriver? TryCreateHybridStorageDriver(IHistoryManager historyManager, StorageDriverDependencies deps);
    }
}
