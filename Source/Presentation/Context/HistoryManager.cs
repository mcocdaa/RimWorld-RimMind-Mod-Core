using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Models;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Context
{
    public class HistoryManager : IHistoryManager
    {
        private readonly ConcurrentDictionary<string, List<HistoryEntry>> _histories =
            new ConcurrentDictionary<string, List<HistoryEntry>>();
        private readonly object _listLock = new object();
        private readonly ITickProvider? _tickProvider;

        private const int MaxEntriesPerNpc = RimMindDefaults.MaxEntriesPerNpc;
        private const int CompressThreshold = RimMindDefaults.HistoryCompressThreshold;

        public HistoryManager(ITickProvider? tickProvider = null)
        {
            _tickProvider = tickProvider;
        }

        private int CurrentTick => _tickProvider?.TicksGame ?? 0;

        public void AddTurn(string npcId, string userMessage, string assistantMessage, string? scenario = null)
        {
            var entries = _histories.GetOrAdd(npcId, _ => new List<HistoryEntry>());
            int tick = CurrentTick;
            lock (_listLock)
            {
                entries.Add(new HistoryEntry("user", userMessage, tick, scenario));
                entries.Add(new HistoryEntry("assistant", assistantMessage, tick, scenario));
            }
        }

        public void AddPendingTurn(
            string npcId,
            string turnId,
            string userMessage,
            string assistantPlaceholder,
            string? scenario = null)
        {
            var entries = _histories.GetOrAdd(npcId, _ => new List<HistoryEntry>());
            int tick = CurrentTick;
            lock (_listLock)
            {
                entries.Add(new HistoryEntry("user", userMessage, tick, scenario, turnId, isPending: true));
                entries.Add(new HistoryEntry("assistant", assistantPlaceholder, tick, scenario, turnId, isPending: true));
            }
        }

        public List<(string role, string content)> GetHistory(string npcId, int maxRounds, string? scenario = null)
            => GetHistorySnapshot(npcId, includePending: false);

        public List<(string role, string content)> GetHistoryForDisplay(
            string npcId,
            int maxRounds,
            string? scenario = null)
            => GetHistorySnapshot(npcId, includePending: true);

        private List<(string role, string content)> GetHistorySnapshot(string npcId, bool includePending)
        {
            if (!_histories.TryGetValue(npcId, out var entries) || entries.Count == 0)
                return new List<(string, string)>();
            List<HistoryEntry> snapshot;
            lock (_listLock) { snapshot = entries.ToList(); }
            return snapshot
                .Where(entry => includePending || !entry.IsPending)
                .Select(entry => (entry.Role, entry.Content))
                .ToList();
        }

        public int GetHistoryCount(string npcId)
        {
            if (!_histories.TryGetValue(npcId, out var entries)) return 0;
            lock (_listLock) { return entries.Count; }
        }

        public void ClearHistory(string npcId)
        {
            _histories.TryRemove(npcId, out _);
        }

        public void CompressIfNeeded(string npcId)
        {
            if (!_histories.TryGetValue(npcId, out var entries)) return;
            lock (_listLock)
            {
                if (entries.Count > MaxEntriesPerNpc)
                {
                    var kept = entries.Skip(entries.Count - CompressThreshold).ToList();
                    entries.Clear();
                    entries.AddRange(kept);
                }
            }
        }

        public void ReplaceLastAssistantTurn(string npcId, string content)
        {
            if (!_histories.TryGetValue(npcId, out var entries)) return;
            lock (_listLock)
            {
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    if (entries[i].Role == "assistant")
                    {
                        entries[i] = new HistoryEntry("assistant", content, entries[i].Tick, entries[i].Scenario);
                        break;
                    }
                }
            }
        }

        public bool ReplaceAssistantTurn(string npcId, string turnId, string content)
        {
            if (!_histories.TryGetValue(npcId, out var entries)) return false;
            lock (_listLock)
            {
                int assistantIndex = entries.FindLastIndex(entry =>
                    entry.Role == "assistant"
                    && entry.IsPending
                    && string.Equals(entry.TurnId, turnId, StringComparison.Ordinal));
                if (assistantIndex < 0)
                    return false;

                HistoryEntry assistant = entries[assistantIndex];
                foreach (HistoryEntry entry in entries)
                {
                    if (entry.IsPending
                        && string.Equals(entry.TurnId, turnId, StringComparison.Ordinal))
                    {
                        entry.IsPending = false;
                        entry.TurnId = null;
                    }
                }
                entries[assistantIndex] = new HistoryEntry(
                    "assistant",
                    content,
                    assistant.Tick,
                    assistant.Scenario,
                    turnId: null,
                    isPending: false);
                return true;
            }
        }

        public bool RemoveTurn(string npcId, string turnId)
        {
            if (!_histories.TryGetValue(npcId, out var entries)) return false;
            lock (_listLock)
            {
                return entries.RemoveAll(entry =>
                    entry.IsPending
                    &&
                    string.Equals(entry.TurnId, turnId, StringComparison.Ordinal)) > 0;
            }
        }

        public string GetAllForSave()
        {
            var dict = GetAllForSaveDict();
            return Newtonsoft.Json.JsonConvert.SerializeObject(dict);
        }

        public Dictionary<string, List<HistoryEntry>> GetAllForSaveDict()
        {
            var result = new Dictionary<string, List<HistoryEntry>>();
            foreach (var kvp in _histories)
            {
                lock (_listLock)
                {
                    result[kvp.Key] = kvp.Value
                        .Where(entry => !entry.IsPending)
                        .ToList();
                }
            }
            return result;
        }

        internal void LoadFromSave(Dictionary<string, List<HistoryEntry>> data)
        {
            _histories.Clear();
            if (data == null) return;
            foreach (var kvp in data)
            {
                _histories[kvp.Key] = kvp.Value;
            }
        }

        public void ExposeData()
        {
            var dict = GetAllForSaveDict();
            global::Verse.Scribe_Collections.Look(ref dict, "histories", Verse.LookMode.Value, Verse.LookMode.Deep);
            if (Verse.Scribe.mode == Verse.LoadSaveMode.LoadingVars)
            {
                LoadFromSave(dict ?? new Dictionary<string, List<HistoryEntry>>());
            }
        }
    }
}
