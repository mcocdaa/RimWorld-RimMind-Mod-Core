using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Verse;

namespace RimMind.Core.Internal
{
    public class IncidentRegistry
    {
        private readonly ConcurrentDictionary<string, Action> _incidentExecutedCallbacks
            = new ConcurrentDictionary<string, Action>();

        private int _callbackCounter;

        public string RegisterIncidentExecutedCallback(Action callback)
        {
            string key = $"cb_{Interlocked.Increment(ref _callbackCounter)}";
            _incidentExecutedCallbacks[key] = callback;
            return key;
        }

        public void NotifyIncidentExecuted()
        {
            foreach (var cb in _incidentExecutedCallbacks.Values.ToList())
            {
                try { cb(); }
                catch (Exception ex) { Log.Warning($"[RimMind-Core] IncidentExecuted callback error: {ex.Message}"); }
            }
        }

        public void UnregisterIncidentExecutedCallback(string key)
            => _incidentExecutedCallbacks.TryRemove(key, out _);

        public void Reset()
        {
            _incidentExecutedCallbacks.Clear();
            _callbackCounter = 0;
        }
    }
}
