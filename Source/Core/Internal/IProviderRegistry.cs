using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimMind.Core.Internal
{
    public interface IProviderRegistry
    {
        void RegisterStaticProvider(string category, string modId, System.Func<string?> provider, int priority);
        void RegisterPawnProvider(string category, string modId, System.Func<Pawn, string?> provider, int priority, bool overrideExisting);
        string? GetProviderData(string category, Pawn pawn);
        string? GetStaticProviderData(string category);
        List<string> GetRegisteredCategories();
        void Reset();
    }
}
