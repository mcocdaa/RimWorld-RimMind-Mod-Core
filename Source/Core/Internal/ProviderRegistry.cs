using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Context;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Context;
using RimMind.Kernel.Prompt;
using Verse;

namespace RimMind.Core.Internal
{
    public class ProviderRegistry : IProviderRegistry
    {
        private readonly ConcurrentDictionary<string, (string modId, Func<string?> provider, int priority)>
            _staticProviders = new ConcurrentDictionary<string, (string, Func<string?>, int)>();

        private readonly ConcurrentDictionary<string, (string modId, Func<Pawn, string?> provider, int priority)>
            _pawnProviders = new ConcurrentDictionary<string, (string, Func<Pawn, string?>, int)>();

        private readonly ConcurrentDictionary<Type, object> _genericProviders = new ConcurrentDictionary<Type, object>();

        public T? GetProvider<T>() where T : class
        {
            return _genericProviders.TryGetValue(typeof(T), out var obj) ? obj as T : null;
        }

        public void RegisterProvider<T>(T provider) where T : class
        {
            _genericProviders[typeof(T)] = provider;
        }

        public IReadOnlyList<string> GetRegisteredProviderNames()
        {
            return _genericProviders.Keys.Select(k => k.Name).ToList();
        }

        public void RegisterStaticProvider(string category, string modId, Func<string?> provider, int priority)
            => _staticProviders[category] = (modId, provider, priority);

        public void RegisterPawnProvider(string category, string modId, Func<object, string?> provider, int priority, bool overrideExisting)
        {
            var pawnProvider = new Func<Pawn, string?>(pawn => provider(pawn));
            if (_pawnProviders.ContainsKey(category) && !overrideExisting) return;
            _pawnProviders[category] = (modId, pawnProvider, priority);
            float priorityFloat = 1.0f - (priority / 10.0f);
            ContextLayer layer = InferLayer(priority);
            var wrappedProvider = new Func<object, List<RimMind.Contracts.Context.ContextEntry>>(pawnObj =>
            {
                var pawn = pawnObj as Pawn;
                if (pawn == null) return new List<RimMind.Contracts.Context.ContextEntry>();
                string? val = pawnProvider(pawn);
                return string.IsNullOrEmpty(val) ? new List<RimMind.Contracts.Context.ContextEntry>() : new List<RimMind.Contracts.Context.ContextEntry> { new RimMind.Contracts.Context.ContextEntry(val!) };
            });
            ContextKeyRegistry.Register(category, layer, priorityFloat, wrappedProvider, modId);
        }

        public string? GetProviderData(string category, object pawn)
        {
            if (!_pawnProviders.TryGetValue(category, out var entry)) return null;
            var typedPawn = pawn as Pawn;
            if (typedPawn == null) return null;
            try { return entry.provider(typedPawn); }
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
            _genericProviders.Clear();
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
