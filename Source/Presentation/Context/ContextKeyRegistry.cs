using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Settings;
using Verse;

namespace RimMind.Presentation.Context
{
    public class ContextKeyRegistry
    {
        private static readonly Dictionary<string, ContextKeyEntry> _keys = new Dictionary<string, ContextKeyEntry>();

        public static void Register(string key, ContextLayer layer, float priority, Func<object?, List<ContextEntry>> factory, string source = "Core")
        {
            if (string.IsNullOrEmpty(key)) return;
            _keys[key] = new ContextKeyEntry(key, layer, priority, factory, source);
        }

        public static bool Unregister(string key)
        {
            return _keys.Remove(key);
        }

        public static IReadOnlyDictionary<string, ContextKeyEntry> GetAllKeys() => _keys;

        public static List<ContextEntry> BuildForPawn(Pawn pawn)
        {
            var result = new List<ContextEntry>();
            foreach (var entry in _keys.Values)
            {
                try
                {
                    var entries = entry.Factory(pawn);
                    if (entries != null) result.AddRange(entries);
                }
                catch { }
            }
            return result;
        }

        public static void Clear()
        {
            _keys.Clear();
        }
    }

    public class ContextKeyEntry
    {
        public string Key { get; }
        public ContextLayer Layer { get; }
        public float Priority { get; }
        public Func<object?, List<ContextEntry>> Factory { get; }
        public string Source { get; }

        public ContextKeyEntry(string key, ContextLayer layer, float priority, Func<object?, List<ContextEntry>> factory, string source)
        {
            Key = key;
            Layer = layer;
            Priority = priority;
            Factory = factory;
            Source = source;
        }
    }
}
