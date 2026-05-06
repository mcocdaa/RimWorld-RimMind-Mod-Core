using RimMind.Core.Client;
using RimMind.Core.Client.Player2;

namespace RimMind.Core.Internal
{
    public interface IClientManager
    {
        IAIClient? GetClient();
        void InvalidateCache();
        Player2Client? GetPlayer2Client();
    }
}
