using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.ValueObjects;
using Verse;

namespace RimMind.Presentation.Runtime
{
    public class ProviderRegistry : IProviderRegistry
    {
        private readonly ConcurrentDictionary<string, Func<object, string?>> _pawnProviders = new ConcurrentDictionary<string, Func<object, string?>>();
        private readonly ConcurrentDictionary<string, Func<string?>> _staticProviders = new ConcurrentDictionary<string, Func<string?>>();
        private readonly ConcurrentDictionary<Type, object> _typedProviders = new ConcurrentDictionary<Type, object>();

        public T? GetProvider<T>() where T : class
        {
            return _typedProviders.TryGetValue(typeof(T), out var provider) ? provider as T : null;
        }

        public void RegisterProvider<T>(T provider) where T : class
        {
            if (provider != null) _typedProviders[typeof(T)] = provider;
        }

        public IReadOnlyList<string> GetRegisteredProviderNames()
        {
            var names = new List<string>();
            foreach (var kv in _typedProviders)
                names.Add(kv.Key.Name);
            return names;
        }

        public void RegisterPawnProvider(string category, string modId, Func<object, string?> provider, int priority, bool overrideExisting)
        {
            if (string.IsNullOrEmpty(category) || provider == null) return;
            if (overrideExisting)
                _pawnProviders[category] = provider;
            else
                _pawnProviders.TryAdd(category, provider);
        }

        public void RegisterStaticProvider(string category, string modId, Func<string?> provider, int priority)
        {
            if (string.IsNullOrEmpty(category) || provider == null) return;
            _staticProviders[category] = provider;
        }

        public Result<string?, RimMindError> GetProviderData(string category, object pawn)
        {
            if (string.IsNullOrEmpty(category))
                return Result<string?, RimMindError>.Err(RimMindErrors.Internal("Category is empty"));

            if (_pawnProviders.TryGetValue(category, out var provider))
            {
                try
                {
                    var data = provider(pawn);
                    return Result<string?, RimMindError>.Ok(data);
                }
                catch (Exception ex)
                {
                    return Result<string?, RimMindError>.Err(RimMindErrors.Internal(ex.Message, ex));
                }
            }

            return Result<string?, RimMindError>.Err(RimMindErrors.Internal($"No provider registered for category: {category}"));
        }

        public Result<string?, RimMindError> GetStaticProviderData(string category)
        {
            if (string.IsNullOrEmpty(category))
                return Result<string?, RimMindError>.Err(RimMindErrors.Internal("Category is empty"));

            if (_staticProviders.TryGetValue(category, out var provider))
            {
                try
                {
                    var data = provider();
                    return Result<string?, RimMindError>.Ok(data);
                }
                catch (Exception ex)
                {
                    return Result<string?, RimMindError>.Err(RimMindErrors.Internal(ex.Message, ex));
                }
            }

            return Result<string?, RimMindError>.Err(RimMindErrors.Internal($"No static provider registered for category: {category}"));
        }

        public List<string> GetRegisteredCategories()
        {
            var categories = new List<string>(_pawnProviders.Keys);
            categories.AddRange(_staticProviders.Keys);
            return categories;
        }

        public void Reset()
        {
            _pawnProviders.Clear();
            _staticProviders.Clear();
            _typedProviders.Clear();
        }
    }
}
