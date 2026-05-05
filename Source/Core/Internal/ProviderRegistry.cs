using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Core.Context;
using RimMind.Core.Prompt;
using RimWorld;
using Verse;

namespace RimMind.Core.Internal
{
    public class ProviderRegistry
    {
        private readonly ConcurrentDictionary<string, (string modId, Func<string?> provider, int priority)>
            _staticProviders = new ConcurrentDictionary<string, (string, Func<string?>, int)>();

        private readonly ConcurrentDictionary<string, (string modId, Func<Pawn, string?> provider, int priority)>
            _pawnProviders = new ConcurrentDictionary<string, (string, Func<Pawn, string?>, int)>();

        public void RegisterStaticProvider(string category, string modId, Func<string?> provider, int priority)
            => _staticProviders[category] = (modId, provider, priority);

        public void RegisterPawnProvider(string category, string modId, Func<Pawn, string?> provider, int priority, bool overrideExisting)
        {
            if (_pawnProviders.ContainsKey(category) && !overrideExisting) return;
            _pawnProviders[category] = (modId, provider, priority);
            float priorityFloat = 1.0f - (priority / 10.0f);
            ContextLayer layer = InferLayer(priority);
            var wrappedProvider = new Func<Pawn, List<ContextEntry>>(pawn =>
            {
                string? val = provider(pawn);
                return string.IsNullOrEmpty(val) ? new List<ContextEntry>() : new List<ContextEntry> { new ContextEntry(val!) };
            });
            ContextKeyRegistry.Register(category, layer, priorityFloat, wrappedProvider, modId);
        }

        public string? GetProviderData(string category, Pawn pawn)
        {
            if (!_pawnProviders.TryGetValue(category, out var entry)) return null;
            try { return entry.provider(pawn); }
            catch (Exception ex) { Log.Warning($"[RimMind-Core] GetProviderData '{category}' error: {ex.Message}"); return null; }
        }

        public string? GetStaticProviderData(string category)
        {
            if (!_staticProviders.TryGetValue(category, out var entry)) return null;
            try { return entry.provider(); }
            catch (Exception ex) { Log.Warning($"[RimMind-Core] GetStaticProviderData '{category}' error: {ex.Message}"); return null; }
        }

        public List<string> GetRegisteredCategories()
        {
            var all = new HashSet<string>();
            all.UnionWith(_staticProviders.Keys);
            all.UnionWith(_pawnProviders.Keys);
            return all.ToList();
        }

        public void Reset()
        {
            _staticProviders.Clear();
            _pawnProviders.Clear();
        }

        private static ContextLayer InferLayer(int priority)
        {
            if (priority <= 1) return ContextLayer.L0_Static;
            if (priority <= 3) return ContextLayer.L1_Baseline;
            if (priority <= 5) return ContextLayer.L2_Environment;
            return ContextLayer.L3_State;
        }
    }
}
