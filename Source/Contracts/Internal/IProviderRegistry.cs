using System.Collections.Generic;

namespace RimMind.Contracts.Internal
{
    public interface IProviderRegistry
    {
        T? GetProvider<T>() where T : class;
        void RegisterProvider<T>(T provider) where T : class;
        IReadOnlyList<string> GetRegisteredProviderNames();
    }
}
