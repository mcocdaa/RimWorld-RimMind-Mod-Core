using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    public sealed class SchemaRegistry
    {
        private readonly ConcurrentDictionary<string, string> _schemas
            = new ConcurrentDictionary<string, string>();
        private readonly ILogSink? _log;

        private static SchemaRegistry? _instance;
        public static SchemaRegistry Instance => _instance ??= new SchemaRegistry();

        public SchemaRegistry(ILogSink? log = null) { _log = log; }

        public void Register(string key, string schema) => _schemas[key] = schema;
        public string? Find(string key) => _schemas.TryGetValue(key, out var s) ? s : null;
        public IReadOnlyDictionary<string, string> All => _schemas;
        public void Reset() => _schemas.Clear();

        public static string PersonalityOutput => Instance.Find("PersonalityOutput") ?? "";
        public static string IncidentOutput => Instance.Find("IncidentOutput") ?? "";
        public static string DarkMemoryOutput => Instance.Find("DarkMemoryOutput") ?? "";
    }
}
