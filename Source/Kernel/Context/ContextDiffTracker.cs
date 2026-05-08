using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Contracts;
using RimMind.Contracts.Context;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Settings;
using RimMind.Contracts.Runtime;
using RimMind.Kernel.Abstractions;

namespace RimMind.Kernel.Context
{
    internal class ContextDiffTracker : IContextDiffTracker
    {
        private readonly Dictionary<string, List<ContextDiff>> _diffStore = new Dictionary<string, List<ContextDiff>>();
        private readonly Dictionary<string, Dictionary<string, string>> _keyLastValues = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, Dictionary<string, float>> _keyLastNumericValues = new Dictionary<string, Dictionary<string, float>>();
        private readonly Dictionary<string, Dictionary<string, float>> _previousNumericValues = new Dictionary<string, Dictionary<string, float>>();

        public IReadOnlyDictionary<string, List<ContextDiff>> DiffStore => _diffStore;
        public IReadOnlyDictionary<string, Dictionary<string, string>> KeyLastValues => _keyLastValues;
        public IReadOnlyDictionary<string, Dictionary<string, float>> KeyLastNumericValues => _keyLastNumericValues;

        private static int CurrentTick => RimMindServiceLocator.Get<ITickProvider>()?.TicksGame ?? 0;

        public void AddDiff(string npcId, string key, string oldValue, string newValue, ContextLayer layer)
        {
            SavePreviousNumericValues(npcId);
            if (ShouldSkipDiff(key, oldValue, newValue, npcId)) return;

            if (!_diffStore.TryGetValue(npcId, out var diffList))
            {
                diffList = new List<ContextDiff>();
                _diffStore[npcId] = diffList;
            }

            var existing = diffList.FirstOrDefault(d => d.Key == key);
            if (existing != null)
            {
                existing.NewValue = newValue;
                existing.Layer = layer;
                existing.ExpireTick = CurrentTick + ContextDiff.DefaultLifetimeTicks;
            }
            else
            {
                diffList.Add(new ContextDiff
                {
                    Key = key,
                    Layer = layer,
                    OldValue = oldValue,
                    NewValue = newValue,
                    InsertedTick = CurrentTick,
                    ExpireTick = CurrentTick + ContextDiff.DefaultLifetimeTicks,
                });
            }
        }

        public void MergeExpiredDiffs(string npcId, IContextCacheManager cacheManager)
        {
            if (!_diffStore.TryGetValue(npcId, out var diffs)) return;

            var expired = diffs.Where(d => d.IsExpired(CurrentTick)).ToList();
            if (expired.Count > 0)
            {
                if (cacheManager.TryGetL1BlockCache(npcId, out var blocks))
                {
                    foreach (var diff in expired)
                    {
                        if (diff.Layer == ContextLayer.L1_Baseline && blocks.TryGetValue(diff.Key, out var blockContent))
                        {
                            if (!string.IsNullOrEmpty(diff.OldValue) && blockContent.Contains(diff.OldValue))
                            {
                                int idx = blockContent.IndexOf(diff.OldValue);
                                blocks[diff.Key] = blockContent.Substring(0, idx) + diff.NewValue + blockContent.Substring(idx + diff.OldValue.Length);
                            }
                            else
                                blocks[diff.Key] = diff.NewValue;
                        }
                        diffs.Remove(diff);
                    }
                }
                else
                {
                    foreach (var diff in expired)
                        diffs.Remove(diff);
                }

                int newVersion = (cacheManager.TryGetL1Version(npcId, out var oldVer) ? oldVer : 0) + 1;
                cacheManager.SetL1Version(npcId, newVersion);

                if (cacheManager.TryGetL1KeyVersions(npcId, out var versions))
                    foreach (var diff in expired.Where(d => d.Layer == ContextLayer.L1_Baseline))
                        versions[diff.Key] = newVersion;
            }
        }

        public void UpdateKeyValues(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IBudgetScheduler scheduler)
        {
            if (pawn == null) return;
            if (!_keyLastValues.ContainsKey(npcId))
                _keyLastValues[npcId] = new Dictionary<string, string>();
            if (!_keyLastNumericValues.ContainsKey(npcId))
                _keyLastNumericValues[npcId] = new Dictionary<string, float>();

            StoreNumericValuesFromPawn(npcId, pawn);

            foreach (var key in keys)
            {
                var entries = key.ValueProvider(pawn);
                string val = EntriesToString(entries);
                if (_keyLastValues[npcId].TryGetValue(key.Key, out var lastVal))
                {
                    if (lastVal != val)
                    {
                        scheduler.OnKeyUpdated(key);
                        if (key.Layer != ContextLayer.L1_Baseline)
                            AddDiff(npcId, key.Key, lastVal, val, key.Layer);
                        cacheManager.EmbedCache.InvalidateBlock(npcId, key.Key);
                        cacheManager.EmbedCache.InvalidateEntries(npcId, key.Key);
                        SemanticEmbedding.InvalidateBlockEmbedding(npcId, key.Key);
                        SemanticEmbedding.InvalidateEntryEmbeddings(npcId, key.Key);
                    }
                }
                _keyLastValues[npcId][key.Key] = val;
            }
        }

        public void StoreNumericValues(string npcId, Dictionary<string, float> values)
        {
            if (!_keyLastNumericValues.ContainsKey(npcId))
                _keyLastNumericValues[npcId] = new Dictionary<string, float>();
            foreach (var kv in values)
                _keyLastNumericValues[npcId][kv.Key] = kv.Value;
        }

        public void ClearNpcDiffs(string npcId)
        {
            _diffStore.Remove(npcId);
        }

        public void RemoveNpcKeyLastValues(string npcId)
        {
            _keyLastValues.Remove(npcId);
            _keyLastNumericValues.Remove(npcId);
            _previousNumericValues.Remove(npcId);
        }

        public void Reset()
        {
            _diffStore.Clear();
            _keyLastValues.Clear();
            _keyLastNumericValues.Clear();
            _previousNumericValues.Clear();
        }

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
            if (!_keyLastValues.ContainsKey(npcId))
                _keyLastValues[npcId] = new Dictionary<string, string>();
            _keyLastValues[npcId][key] = value;
        }

        private bool ShouldSkipDiff(string key, string oldValue, string newValue, string npcId)
        {
            if (string.IsNullOrEmpty(oldValue) || string.IsNullOrEmpty(newValue)) return false;
            if (key == "mood" && _keyLastNumericValues.TryGetValue(npcId, out var moodNums))
            {
                float moodThreshold = RimMindModAccessor.Settings?.Context?.moodDiffThreshold ?? 5f;
                if (moodNums.TryGetValue("mood", out var currentMood)
                    && _previousNumericValues.TryGetValue(npcId, out var prevNums)
                    && prevNums.TryGetValue("mood", out var prevMood)
                    && Math.Abs(currentMood - prevMood) < moodThreshold) return true;
            }
            if (key == "current_area" && _keyLastNumericValues.TryGetValue(npcId, out var areaNums))
            {
                float tempThreshold = RimMindModAccessor.Settings?.Context?.temperatureDiffThreshold ?? 5f;
                if (areaNums.TryGetValue("current_area", out var currentTemp)
                    && _previousNumericValues.TryGetValue(npcId, out var prevNums2)
                    && prevNums2.TryGetValue("current_area", out var prevTemp)
                    && Math.Abs(currentTemp - prevTemp) < tempThreshold) return true;
            }
            return false;
        }

        private void StoreNumericValuesFromPawn(string npcId, object pawn)
        {
            var data = RimMindServiceLocator.Get<IPawnDataExtractor>()?.Extract(pawn) ?? new PawnExtractedData();
            var dict = _keyLastNumericValues[npcId];
            if (data.MoodString != null)
                dict["mood"] = data.MoodPercent;
            if (data.HasMap)
                dict["current_area"] = data.Temperature;
        }

        private void SavePreviousNumericValues(string npcId)
        {
            if (!_keyLastNumericValues.TryGetValue(npcId, out var current)) return;
            if (!_previousNumericValues.ContainsKey(npcId))
                _previousNumericValues[npcId] = new Dictionary<string, float>();
            foreach (var kv in current)
                _previousNumericValues[npcId][kv.Key] = kv.Value;
        }

        private static string EntriesToString(List<ContextEntry>? entries)
        {
            if (entries == null || entries.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Content))
                    sb.AppendLine(entry.Content);
            }
            return sb.ToString().TrimEnd();
        }
    }
}
