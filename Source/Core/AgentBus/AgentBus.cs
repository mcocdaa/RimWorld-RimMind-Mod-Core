using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMind.Core.AgentBus
{
    public enum AgentBusEventType
    {
        Perception,
        Decision,
        Action,
        Goal,
        Lifecycle
    }

    public class AgentBusEvent
    {
        public string NpcId = "";
        public int PawnId;
        public AgentBusEventType EventType;
        public int Timestamp;

        public AgentBusEvent() { }

        public AgentBusEvent(AgentBusEventType eventType, string npcId)
        {
            EventType = eventType;
            NpcId = npcId;
        }
    }

    public static class AgentBus
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>> _handlers
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>>();

        private static readonly ConcurrentQueue<AgentBusEvent> _backgroundQueue
            = new ConcurrentQueue<AgentBusEvent>();

        private static int _autoKeyCounter;

        public static void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent
        {
            if (handler == null || string.IsNullOrEmpty(key)) return;
            var type = typeof(T);
            var dict = _handlers.GetOrAdd(type, _ => new ConcurrentDictionary<string, Delegate>());
            dict[key] = handler;
        }

        public static string Subscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            if (handler == null) return "";
            string key = $"auto_{System.Threading.Interlocked.Increment(ref _autoKeyCounter)}";
            Subscribe(key, handler);
            return key;
        }

        public static void Unsubscribe<T>(string key) where T : AgentBusEvent
        {
            if (string.IsNullOrEmpty(key)) return;
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var dict))
                dict.TryRemove(key, out _);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var dict)) return;
            string? keyToRemove = null;
            foreach (var kvp in dict)
            {
                if (kvp.Value is Action<T> existing && existing.Equals(handler))
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }
            if (keyToRemove != null)
                dict.TryRemove(keyToRemove, out _);
        }

        public static void Publish<T>(T evt) where T : AgentBusEvent
        {
            if (evt == null) return;
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var dict)) return;
            var snapshot = dict.ToArray();
            foreach (var kvp in snapshot)
            {
                try
                {
                    if (kvp.Value is Action<T> action)
                        action(evt);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimMind-Core] AgentBus handler error for {typeof(T).Name}: {ex.Message}");
                }
            }
        }

        public static void Publish(AgentBusEvent evt)
        {
            if (evt == null) return;
            var type = evt.GetType();
            if (!_handlers.TryGetValue(type, out var dict)) return;
            var snapshot = dict.ToArray();
            foreach (var kvp in snapshot)
            {
                try
                {
                    if (kvp.Value is Delegate del && del.Method.GetParameters().Length == 1
                        && del.Method.GetParameters()[0].ParameterType.IsAssignableFrom(type))
                    {
                        del.DynamicInvoke(evt);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimMind-Core] AgentBus handler error for {type.Name}: {ex.Message}");
                }
            }
        }

        public static void PublishFromBackground<T>(T evt) where T : AgentBusEvent
        {
            if (evt == null) return;
            _backgroundQueue.Enqueue(evt);
        }

        public static void FlushBackgroundQueue()
        {
            while (_backgroundQueue.TryDequeue(out var evt))
                Publish(evt);
        }

        internal static ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>> GetHandlers()
            => _handlers;

        internal static int GetBackgroundQueueCount() => _backgroundQueue.Count;

        public static void ClearAllSubscribers()
        {
            foreach (var kvp in _handlers)
                kvp.Value.Clear();
            _handlers.Clear();
        }
    }
}
