using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Abstractions;

namespace RimMind.Kernel.Context
{
    public class HistoryEntry : Verse.IExposable
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
            Verse.Scribe_Values.Look(ref Role, "role");
            Verse.Scribe_Values.Look(ref Content, "content");
            Verse.Scribe_Values.Look(ref Tick, "tick");
            Verse.Scribe_Values.Look(ref Scenario, "scenario", null!);
        }
    }

    public class HistoryManager : IHistoryManager
    {
        public static IHistoryManager? Instance
        {
            get => RimMindServiceLocator.Get<IHistoryManager>();
            private set
            {
                if (value != null)
                    RimMindServiceLocator.Register<IHistoryManager>(value);
            }
        }

        private readonly ConcurrentDictionary<string, List<HistoryEntry>> _histories =
            new ConcurrentDictionary<string, List<HistoryEntry>>();
        private readonly object _listLock = new object();

        private const int MaxEntriesPerNpc = 200;
        private const int CompressThreshold = 150;

        private static int CurrentTick => RimMindServiceLocator.Get<ITickProvider>()?.TicksGame ?? 0;

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
            Verse.Scribe_Collections.Look(ref dict, "histories", Verse.LookMode.Value, Verse.LookMode.Deep);
            if (Verse.Scribe.mode == Verse.LoadSaveMode.LoadingVars)
            {
                LoadFromSave(dict ?? new Dictionary<string, List<HistoryEntry>>());
            }
        }
    }
}
