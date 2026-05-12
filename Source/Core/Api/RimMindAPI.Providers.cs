using RimMind.Contracts.Result;
using RimMind.Core.Runtime;
using Verse;
using System.Collections.Generic;

namespace RimMind.Core
{
    public static partial class RimMindAPI
    {
        public static class Providers
        {
            public static Result<string?, RimMindError> GetProviderData(string category, Pawn pawn)
                => RimMindRuntime.Instance.ProviderRegistry.GetProviderData(category, pawn);

            public static Result<string?, RimMindError> GetStaticProviderData(string category)
                => RimMindRuntime.Instance.ProviderRegistry.GetStaticProviderData(category);

            public static List<string> GetRegisteredCategories()
                => RimMindRuntime.Instance.ProviderRegistry.GetRegisteredCategories();
        }
    }
}
