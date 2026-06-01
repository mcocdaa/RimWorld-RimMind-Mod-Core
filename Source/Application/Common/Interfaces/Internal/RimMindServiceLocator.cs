using System;
using System.Collections.Concurrent;

namespace RimMind.Application.Common.Interfaces.Internal
{
    public static class RimMindServiceLocator
    {
        private static readonly ConcurrentDictionary<Type, object> _services
            = new ConcurrentDictionary<Type, object>();

        public static Action<string>? OnServiceNotFound { get; set; }

        public static void Register<T>(T instance) where T : class
            => _services[typeof(T)] = instance;

        public static T? Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var svc))
                return (T)svc;
            OnServiceNotFound?.Invoke($"[RimMind-Core] Service not registered: {typeof(T).Name}");
            return null;
        }

        public static T? TryGet<T>() where T : class
            => _services.TryGetValue(typeof(T), out var svc) ? (T)svc : null;

        public static bool IsRegistered<T>()
            => _services.ContainsKey(typeof(T));

        public static void Reset()
            => _services.Clear();
    }
}
