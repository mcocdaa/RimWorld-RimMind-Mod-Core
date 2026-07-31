using RimMind.Application.Common.Interfaces.Internal;

namespace RimMind.Application.Common.Interfaces.Client
{
    public interface IAIClientFactory : Extension.IExtension
    {
        string ProviderId { get; }
        bool RequiresApiKey { get; }
        IAIClient Create(ISettingsProvider settings);
    }
}
