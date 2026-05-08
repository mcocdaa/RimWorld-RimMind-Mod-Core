using RimMind.Contracts.Client;

namespace RimMind.Contracts.Internal
{
    public interface IClientManager
    {
        IAIClient? GetClient();
        void InvalidateCache();
        object? GetPlayer2Client();
    }
}
