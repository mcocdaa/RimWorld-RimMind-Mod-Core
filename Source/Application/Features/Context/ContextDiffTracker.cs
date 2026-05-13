using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal sealed class ContextDiffTracker : IContextDiffTracker
    {
        private readonly ConcurrentDictionary<string, List<ContextDiff>> _diffStore
            = new ConcurrentDictionary<string, List<ContextDiff>>();
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _keyLastValues
            = new ConcurrentDictionary<string, Dictionary<string, string>>();
        private readonly ConcurrentDictionary<string, Dictionary<string, float>> _keyLastNumericValues
            = new ConcurrentDictionary<string, Dictionary<string, float>>();
        private readonly ILogSink? _log;

        public ContextDiffTracker(ILogSink? log = null) { _log = log; }

        public IReadOnlyDictionary<string, List<ContextDiff>> DiffStore => _diffStore;
        public IReadOnlyDictionary<string, Dictionary<string, string>> KeyLastValues => _keyLastValues;
        public IReadOnlyDictionary<string, Dictionary<string, float>> KeyLastNumericValues => _keyLastNumericValues;

        public void AddDiff(string npcId, string key, string oldValue, string newValue, ContextLayer layer)
        {
            var diff = new ContextDiff
            {
                Key = key,
                OldValue = oldValue,
                NewValue = newValue,
                Layer = layer
            };
            var list = _diffStore.GetOrAdd(npcId, _ => new List<ContextDiff>());
            lock (list) { list.Add(diff); }
        }

        public void MergeExpiredDiffs(string npcId, IContextCacheManager cacheManager) { }
        public void UpdateKeyValues(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IBudgetScheduler scheduler) { }
        public void StoreNumericValues(string npcId, Dictionary<string, float> values)
        {
            _keyLastNumericValues[npcId] = new Dictionary<string, float>(values);
        }

        public void ClearNpcDiffs(string npcId) => _diffStore.TryRemove(npcId, out _);
        public void RemoveNpcKeyLastValues(string npcId) => _keyLastValues.TryRemove(npcId, out _);
        public void Reset() { _diffStore.Clear(); _keyLastValues.Clear(); _keyLastNumericValues.Clear(); }
        public int GetDiffStoreCount() => _diffStore.Count;

        public bool TryGetDiffStore(string npcId, out List<ContextDiff> diffs)
        {
            return _diffStore.TryGetValue(npcId, out diffs!);
        }

        public bool TryGetKeyLastValues(string npcId, out Dictionary<string, string> values)
        {
            return _keyLastValues.TryGetValue(npcId, out values!);
        }

        public void SetKeyLastValue(string npcId, string key, string value)
        {
            var dict = _keyLastValues.GetOrAdd(npcId, _ => new Dictionary<string, string>());
            lock (dict) { dict[key] = value; }
        }
    }
}
