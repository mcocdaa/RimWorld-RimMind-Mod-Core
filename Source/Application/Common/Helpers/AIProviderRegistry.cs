using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;

namespace RimMind.Application.Common.Helpers
{
    public static class AIProviderRegistry
    {
        private static readonly string FallbackProvider = "openai";

        public static IReadOnlyList<string> GetAllProviderIds(IExtensionRegistry<IAIClientFactory>? registry = null)
        {
            if (registry == null || registry.All.Count == 0)
                return new List<string> { FallbackProvider };
            return registry.All.Select(f => f.ProviderId).ToList();
        }

        public static string GetDefaultProviderId(IExtensionRegistry<IAIClientFactory>? registry = null)
        {
            var ids = GetAllProviderIds(registry);
            return ids.Count > 0 ? ids[0] : FallbackProvider;
        }

        public static bool IsProviderRegistered(string providerId, IExtensionRegistry<IAIClientFactory>? registry = null)
        {
            if (string.IsNullOrEmpty(providerId)) return false;
            return registry?.FindById(providerId) != null;
        }

        public static bool RequiresApiKey(string providerId, IExtensionRegistry<IAIClientFactory>? registry = null)
        {
            if (string.IsNullOrEmpty(providerId)) return true;
            var factory = registry?.FindById(providerId);
            return factory?.RequiresApiKey ?? true;
        }
    }
}
