using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal sealed class SchemaRegistry
    {
        private readonly ConcurrentDictionary<string, string> _schemas
            = new ConcurrentDictionary<string, string>();
        private readonly ILogSink? _log;

        public SchemaRegistry(ILogSink? log = null) { _log = log; }

        public void Register(string key, string schema) => _schemas[key] = schema;
        public string? Find(string key) => _schemas.TryGetValue(key, out var s) ? s : null;
        public IReadOnlyDictionary<string, string> All => _schemas;
        public void Reset() => _schemas.Clear();
    }
}
