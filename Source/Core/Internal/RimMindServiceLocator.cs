using System;
using System.Collections.Concurrent;

namespace RimMind.Core.Internal
{
    public static class RimMindServiceLocator
    {
        private static readonly ConcurrentDictionary<Type, object> _services
            = new ConcurrentDictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
            => _services[typeof(T)] = instance;

        public static T? Get<T>() where T : class
            => _services.TryGetValue(typeof(T), out var svc) ? (T)svc : null;

        public static bool IsRegistered<T>()
            => _services.ContainsKey(typeof(T));

        public static void Reset()
            => _services.Clear();
    }
}
