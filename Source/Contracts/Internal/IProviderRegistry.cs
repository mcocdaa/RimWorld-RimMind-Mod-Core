using System;
using System.Collections.Generic;

namespace RimMind.Contracts.Internal
{
    public interface IProviderRegistry
    {
        T? GetProvider<T>() where T : class;
        void RegisterProvider<T>(T provider) where T : class;
        IReadOnlyList<string> GetRegisteredProviderNames();
        void RegisterStaticProvider(string category, string modId, Func<string?> provider, int priority);
        void RegisterPawnProvider(string category, string modId, Func<object, string?> provider, int priority, bool overrideExisting);
        string? GetProviderData(string category, object pawn);
        string? GetStaticProviderData(string category);
        List<string> GetRegisteredCategories();
        void Reset();
    }
}
