using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using Verse;

namespace RimMind.Presentation.Context
{
    public class HistoryManager : IHistoryManager
    {
        private readonly Dictionary<string, List<ChatMessage>> _histories = new Dictionary<string, List<ChatMessage>>();
        private readonly Dictionary<string, int> _lastAccessTick = new Dictionary<string, int>();
        private const int MaxHistoryPerNpc = 50;
        private const int MaxTotalNpcs = 100;

        public void AddMessage(string npcId, ChatMessage message)
        {
            if (string.IsNullOrEmpty(npcId) || message == null) return;
            if (!_histories.TryGetValue(npcId, out var history))
            {
                if (_histories.Count >= MaxTotalNpcs)
                    EvictOldest();
                history = new List<ChatMessage>();
                _histories[npcId] = history;
            }
            history.Add(message);
            while (history.Count > MaxHistoryPerNpc)
                history.RemoveAt(0);
            _lastAccessTick[npcId] = Find.TickManager.TicksGame;
        }

        public IReadOnlyList<ChatMessage> GetHistory(string npcId)
        {
            if (string.IsNullOrEmpty(npcId) || !_histories.TryGetValue(npcId, out var history))
                return new List<ChatMessage>();
            _lastAccessTick[npcId] = Find.TickManager.TicksGame;
            return history;
        }

        public void ClearHistory(string npcId)
        {
            if (!string.IsNullOrEmpty(npcId))
            {
                _histories.Remove(npcId);
                _lastAccessTick.Remove(npcId);
            }
        }

        public void ClearAll()
        {
            _histories.Clear();
            _lastAccessTick.Clear();
        }

        public void ExposeData()
        {
            var keys = new List<string>(_histories.Keys);
            Scribe_Collections.Look(ref keys, "historyKeys", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                _histories.Clear();
                _lastAccessTick.Clear();
            }
        }

        private void EvictOldest()
        {
            string? oldestKey = null;
            int oldestTick = int.MaxValue;
            foreach (var kv in _lastAccessTick)
            {
                if (kv.Value < oldestTick)
                {
                    oldestTick = kv.Value;
                    oldestKey = kv.Key;
                }
            }
            if (oldestKey != null)
            {
                _histories.Remove(oldestKey);
                _lastAccessTick.Remove(oldestKey);
            }
        }
    }
}
