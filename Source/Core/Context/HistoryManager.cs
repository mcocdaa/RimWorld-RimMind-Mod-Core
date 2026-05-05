using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMind.Core.Context
{
    public class HistoryEntry : IExposable
    {
        public string Role = "";
        public string Content = "";
        public int Tick;
        public string? Scenario;

        public HistoryEntry() { }

        public HistoryEntry(string role, string content, int tick, string? scenario = null)
        {
            Role = role;
            Content = content;
            Tick = tick;
            Scenario = scenario;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Role, "role");
            Scribe_Values.Look(ref Content, "content");
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref Scenario, "scenario", null);
        }
    }

    public class HistoryManager
    {
        public static HistoryManager Instance { get; private set; } = new HistoryManager();

        private readonly ConcurrentDictionary<string, List<HistoryEntry>> _histories =
            new ConcurrentDictionary<string, List<HistoryEntry>>();
        private readonly object _listLock = new object();

        private const int MaxEntriesPerNpc = 200;
        private const int CompressThreshold = 150;

        public void AddTurn(string npcId, string userMessage, string assistantMessage, string? scenario = null)
        {
            var entries = _histories.GetOrAdd(npcId, _ => new List<HistoryEntry>());
            int tick = Find.TickManager?.TicksGame ?? 0;
            lock (_listLock)
            {
                entries.Add(new HistoryEntry("user", userMessage, tick, scenario));
                entries.Add(new HistoryEntry("assistant", assistantMessage, tick, scenario));
            }
        }

        public List<(string role, string content)> GetHistory(string npcId, int maxRounds, string? scenario = null)
        {
            if (!_histories.TryGetValue(npcId, out var entries) || entries.Count == 0)
                return new List<(string, string)>();
            List<HistoryEntry> snapshot;
            lock (_listLock) { snapshot = entries.ToList(); }
            return snapshot.Select(e => (e.Role, e.Content)).ToList();
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

        internal Dictionary<string, List<HistoryEntry>> GetAllForSave()
        {
            var result = new Dictionary<string, List<HistoryEntry>>();
            foreach (var kvp in _histories)
            {
                lock (_listLock)
                {
                    result[kvp.Key] = kvp.Value.ToList();
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
            var dict = GetAllForSave();
            Scribe_Collections.Look(ref dict, "histories", LookMode.Value, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                LoadFromSave(dict ?? new Dictionary<string, List<HistoryEntry>>());
            }
        }
    }
}
