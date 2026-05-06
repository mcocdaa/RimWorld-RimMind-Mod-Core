using System;
using System.Collections.Concurrent;
using System.Linq;
using RimMind.Contracts;
using Verse;

namespace RimMind.Core.AgentBus
{
    internal sealed class AgentBusImpl : IAgentBus
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>> _handlers
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>>();

        private readonly ConcurrentQueue<PendingEvent> _backgroundQueue
            = new ConcurrentQueue<PendingEvent>();

        private int _autoKeyCounter;

        private readonly struct PendingEvent
        {
            public readonly AgentBusEvent Event;
            public readonly Action<AgentBusEvent> Invoker;

            public PendingEvent(AgentBusEvent evt, Action<AgentBusEvent> invoker)
            {
                Event = evt;
                Invoker = invoker;
            }
        }

        public string Subscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            AssertMainThread();
            if (handler == null) return "";
            string key = $"auto_{System.Threading.Interlocked.Increment(ref _autoKeyCounter)}";
            Subscribe(key, handler);
            return key;
        }

        public void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent
        {
            AssertMainThread();
            if (handler == null || string.IsNullOrEmpty(key)) return;
            var type = typeof(T);
            var dict = _handlers.GetOrAdd(type, _ => new ConcurrentDictionary<string, Delegate>());
            dict[key] = handler;
        }

        public void Unsubscribe<T>(string key) where T : AgentBusEvent
        {
            AssertMainThread();
            if (string.IsNullOrEmpty(key)) return;
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var dict))
                dict.TryRemove(key, out _);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            AssertMainThread();
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

        public void Publish<T>(T evt) where T : AgentBusEvent
        {
            AssertMainThread();
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

        public void PublishFromBackground<T>(T evt) where T : AgentBusEvent
        {
            if (evt == null) return;
            Action<AgentBusEvent> invoker = e => DispatchToHandlers((T)e);
            _backgroundQueue.Enqueue(new PendingEvent(evt, invoker));
        }

        private void DispatchToHandlers<T>(T evt) where T : AgentBusEvent
        {
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

        public void FlushBackgroundQueue()
        {
            AssertMainThread();
            while (_backgroundQueue.TryDequeue(out var pending))
            {
                try
                {
                    pending.Invoker(pending.Event);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimMind-Core] AgentBus background handler error: {ex.Message}");
                }
            }
        }

        public void ClearAllSubscribers()
        {
            foreach (var kvp in _handlers)
                kvp.Value.Clear();
            _handlers.Clear();
        }

        internal int GetHandlerCount() => _handlers.Count;
        internal int GetBackgroundQueueCount() => _backgroundQueue.Count;

        private static void AssertMainThread()
        {
#if DEBUG
            if (!UnityData.IsInMainThread)
                throw new InvalidOperationException(
                    "[RimMind-Core] AgentBus must be called on main thread; use PublishFromBackground for background calls");
#endif
        }
    }
}
