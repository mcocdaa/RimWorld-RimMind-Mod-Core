using RimMind.Application.Common.Interfaces.Internal;

namespace RimMind.Application.Common.Interfaces.Client
{
    public interface IAIClientFactory : Extension.IExtension
    {
        string ProviderId { get; }
        IAIClient Create(ISettingsProvider settings);
    }
}
