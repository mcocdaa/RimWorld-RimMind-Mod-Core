using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RimMind.Kernel.Context
{
    public static class SchemaRegistry
    {
        private static readonly ConcurrentDictionary<string, string> _schemas
            = new ConcurrentDictionary<string, string>();

        public static void Register(string key, string schemaJson)
        {
            _schemas[key] = schemaJson;
        }

        public static bool Unregister(string key)
        {
            return _schemas.TryRemove(key, out _);
        }

        public static string? Get(string key)
        {
            return _schemas.TryGetValue(key, out var schema) ? schema : null;
        }

        public static bool Contains(string key)
        {
            return _schemas.ContainsKey(key);
        }

        public static Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(_schemas);
        }

        public static void Clear()
        {
            _schemas.Clear();
        }

        public static string AgentDecision => Get("AgentDecision") ?? "";
        public static string Dialogue => Get("Dialogue") ?? "";
    }
}
