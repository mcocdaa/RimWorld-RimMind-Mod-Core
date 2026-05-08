using System;
using System.Collections.Concurrent;
using System.Linq;
using RimMind.Contracts;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Abstractions;
using RimMind.Kernel.Logging;
using ContractsAgentBusEvent = RimMind.Contracts.AgentBusEvent;

namespace RimMind.Kernel.Bus
{
    public sealed class AgentBusImpl : IAgentBus
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>> _handlers
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>>();

        private readonly ConcurrentQueue<PendingEvent> _backgroundQueue
            = new ConcurrentQueue<PendingEvent>();

        private int _autoKeyCounter;

        private Func<ContractsAgentBusEvent, Delegate[], bool, bool>? _publishViaPipeline;

        private readonly struct PendingEvent
        {
            public readonly ContractsAgentBusEvent Event;
            public readonly Action<ContractsAgentBusEvent> Invoker;

            public PendingEvent(ContractsAgentBusEvent evt, Action<ContractsAgentBusEvent> invoker)
            {
                Event = evt;
                Invoker = invoker;
            }
        }

        internal void SetPublishViaPipeline(Func<ContractsAgentBusEvent, Delegate[], bool, bool> publishViaPipeline)
        {
            _publishViaPipeline = publishViaPipeline;
        }

        public string Subscribe<T>(Action<T> handler) where T : ContractsAgentBusEvent
        {
            AssertMainThread();
            if (handler == null) return "";
            string key = $"auto_{System.Threading.Interlocked.Increment(ref _autoKeyCounter)}";
            Subscribe(key, handler);
            return key;
        }

        public void Subscribe<T>(string key, Action<T> handler) where T : ContractsAgentBusEvent
        {
            AssertMainThread();
            if (handler == null || string.IsNullOrEmpty(key)) return;
            var type = typeof(T);
            var dict = _handlers.GetOrAdd(type, _ => new ConcurrentDictionary<string, Delegate>());
            dict[key] = handler;
        }

        public void Unsubscribe<T>(string key) where T : ContractsAgentBusEvent
        {
            AssertMainThread();
            if (string.IsNullOrEmpty(key)) return;
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var dict))
                dict.TryRemove(key, out _);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : ContractsAgentBusEvent
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

        public void Publish<T>(T evt) where T : ContractsAgentBusEvent
        {
            AssertMainThread();
            if (evt == null) return;

            if (_publishViaPipeline != null)
            {
                var subscribers = GetSubscribersSnapshot<T>();
                _publishViaPipeline(evt, subscribers, false);
                return;
            }

            DispatchToHandlers(evt);
        }

        public void PublishFromBackground<T>(T evt) where T : ContractsAgentBusEvent
        {
            if (evt == null) return;
            Action<ContractsAgentBusEvent> invoker = e =>
            {
                var typed = (T)e;
                if (_publishViaPipeline != null)
                {
                    var subscribers = GetSubscribersSnapshot<T>();
                    _publishViaPipeline(typed, subscribers, true);
                }
                else
                {
                    DispatchToHandlers(typed);
                }
            };
            _backgroundQueue.Enqueue(new PendingEvent(evt, invoker));
        }

        private Delegate[] GetSubscribersSnapshot<T>() where T : ContractsAgentBusEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var dict)) return Array.Empty<Delegate>();
            return dict.Values.ToArray();
        }

        private void DispatchToHandlers<T>(T evt) where T : ContractsAgentBusEvent
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
                    RimMindLogger.Warning($"AgentBus handler error for {typeof(T).Name}: {ex.Message}");
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
                    RimMindLogger.Warning($"AgentBus background handler error: {ex.Message}");
                }
            }
        }

        public void ClearAllSubscribers()
        {
            foreach (var kvp in _handlers)
                kvp.Value.Clear();
            _handlers.Clear();
        }

        public int GetHandlerCount() => _handlers.Count;
        public int GetBackgroundQueueCount() => _backgroundQueue.Count;

        private static void AssertMainThread()
        {
#if DEBUG
            if (!(RimMindServiceLocator.Get<IThreadChecker>()?.IsMainThread ?? true))
                throw new InvalidOperationException(
                    "[RimMind-Core] AgentBus must be called on main thread; use PublishFromBackground for background calls");
#endif
        }
    }
}
