using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Runtime.Services;
using Verse;
using System.Collections.Generic;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class Providers
        {
            private static readonly RuntimeServiceRef<IProviderRegistry> Registries =
                RuntimeServiceRef<IProviderRegistry>.Required();

            public static Result<string?, RimMindError> GetProviderData(string category, Pawn pawn)
                => Registries.Value.GetProviderData(category, pawn);

            public static Result<string?, RimMindError> GetStaticProviderData(string category)
                => Registries.Value.GetStaticProviderData(category);

            public static List<string> GetRegisteredCategories()
                => Registries.Value.GetRegisteredCategories();

            public static int UnregisterByOwner(string ownerModId)
                => Registries.Value.UnregisterByOwner(ownerModId);
        }
    }
}
