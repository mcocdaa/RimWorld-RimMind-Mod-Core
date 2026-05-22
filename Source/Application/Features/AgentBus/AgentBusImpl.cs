using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Bus;
using RimMind.Domain.Common;
using RimMind.Domain.Events;

namespace RimMind.Application.Features.AgentBus
{
    public sealed class AgentBusImpl : IAgentBus
    {
        public event Action? SubscribersCleared;

        private readonly ConcurrentDictionary<Type, List<HandlerEntry>> _handlers
            = new ConcurrentDictionary<Type, List<HandlerEntry>>();
        private readonly ConcurrentQueue<DeferredPublish> _backgroundQueue
            = new ConcurrentQueue<DeferredPublish>();
        private readonly ILogSink? _log;
        private readonly IThreadChecker? _threadChecker;
        private IPipeline<BusPublishContext>? _pipeline;
        private int _handlerIdCounter;

        public AgentBusImpl(ILogSink? log = null, IThreadChecker? threadChecker = null)
        {
            _log = log;
            _threadChecker = threadChecker;
        }

        public void SetPipeline(IPipeline<BusPublishContext> pipeline)
        {
            _pipeline = pipeline;
        }

        public Action<AgentBusEvent>? DispatchAction => DispatchToHandlers;

        internal void DispatchToHandlers(AgentBusEvent evt)
        {
            if (evt == null) return;
            if (!_handlers.TryGetValue(evt.GetType(), out var list) || list.Count == 0) return;
            HandlerEntry[] snapshot;
            lock (list) { snapshot = list.ToArray(); }
            foreach (var entry in snapshot)
            {
                try { entry.Action(evt); }
                catch (Exception ex)
                {
                    var errorMsg = $"AgentBus handler error: {ex}";
                    if (_log != null)
                        _log.Error(errorMsg);
                    else
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                }
            }
        }

        public string Subscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            var key = $"auto_{Interlocked.Increment(ref _handlerIdCounter)}";
            Subscribe(key, handler);
            return key;
        }

        public void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent
        {
            var entry = new HandlerEntry(key, h => handler((T)h));
            _handlers.AddOrUpdate(
                typeof(T),
                _ => new List<HandlerEntry> { entry },
                (_, list) => { lock (list) { list.Add(entry); } return list; });
        }

        public void Unsubscribe<T>(string key) where T : AgentBusEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                lock (list)
                {
                    list.RemoveAll(e => e.Key == key);
                }
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                lock (list)
                {
                    list.RemoveAll(e => e.Action.Target == handler.Target && e.Action.Method == handler.Method);
                }
            }
        }

        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        public void Publish<T>(T evt) where T : AgentBusEvent
        {
            if (evt == null) return;
            if (_pipeline != null)
            {
                var context = new BusPublishContext(evt);
                _pipeline.ExecuteAsync(context).GetAwaiter().GetResult();
                return;
            }
            DispatchToHandlers(evt);
        }

        [ThreadAffinity(ThreadAffinityKind.Any)]
        public void PublishFromBackground<T>(T evt) where T : AgentBusEvent
        {
            if (evt == null) return;
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0) return;
            _backgroundQueue.Enqueue(new DeferredPublish(typeof(T), evt));
        }

        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        public void FlushBackgroundQueue()
        {
            while (_backgroundQueue.TryDequeue(out var deferred))
            {
                if (deferred.Event is AgentBusEvent evt)
                {
                    if (_pipeline != null)
                    {
                        var context = new BusPublishContext(evt);
                        _pipeline.ExecuteAsync(context).GetAwaiter().GetResult();
                    }
                    else
                    {
                        DispatchToHandlers(evt);
                    }
                }
            }
        }

        public void ClearAllSubscribers()
        {
            _handlers.Clear();
            SubscribersCleared?.Invoke();
        }

        public int GetHandlerCount()
        {
            int count = 0;
            foreach (var kvp in _handlers)
            {
                lock (kvp.Value) { count += kvp.Value.Count; }
            }
            return count;
        }

        public int GetBackgroundQueueCount() => _backgroundQueue.Count;

        private sealed class HandlerEntry
        {
            public string Key;
            public Action<object> Action;
            public HandlerEntry(string key, Action<object> action) { Key = key; Action = action; }
        }

        private sealed class DeferredPublish
        {
            public Type EventType;
            public object Event;
            public DeferredPublish(Type type, object evt) { EventType = type; Event = evt; }
        }
    }
}
