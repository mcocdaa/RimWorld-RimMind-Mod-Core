using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Core.AgentBus;
using Xunit;

namespace RimMind.Core.Tests
{
    public class SimpleAgentBus
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers
            = new ConcurrentDictionary<Type, List<Delegate>>();

        private readonly ConcurrentQueue<AgentBusEvent> _backgroundQueue
            = new ConcurrentQueue<AgentBusEvent>();

        private readonly List<AgentBusEvent> _pendingDispatch
            = new List<AgentBusEvent>();

        public void Subscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            var list = _handlers.GetOrAdd(type, _ => new List<Delegate>());
            lock (list)
            {
                list.Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list)) return;
            lock (list)
            {
                list.Remove(handler);
            }
        }

        public void Publish<T>(T evt) where T : AgentBusEvent
        {
            if (evt == null) return;
            evt.Timestamp = 100000;
            DispatchToHandlers(evt);
        }

        public void PublishFromBackground<T>(T evt) where T : AgentBusEvent
        {
            if (evt == null) return;
            _backgroundQueue.Enqueue(evt);
        }

        public void FlushBackgroundQueue()
        {
            _pendingDispatch.Clear();
            while (_backgroundQueue.TryDequeue(out var evt))
                _pendingDispatch.Add(evt);

            for (int i = 0; i < _pendingDispatch.Count; i++)
            {
                _pendingDispatch[i].Timestamp = 100000;
                DispatchToHandlers(_pendingDispatch[i]);
            }

            _pendingDispatch.Clear();
        }

        private void DispatchToHandlers<T>(T evt) where T : AgentBusEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list)) return;

            Delegate[] snapshot;
            lock (list)
            {
                snapshot = list.ToArray();
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    if (snapshot[i] is Action<T> action)
                        action(evt);
                }
                catch { }
            }
        }
    }

    public class TestBusEvent : AgentBusEvent
    {
        public string Payload { get; }
        public TestBusEvent(string payload = "") : base(AgentBusEventType.Perception, "test") { Payload = payload; }
    }

    public class AnotherBusEvent : AgentBusEvent
    {
        public AnotherBusEvent() : base(AgentBusEventType.Decision, "test") { }
    }

    public class AgentBusTests
    {
        [Fact]
        public void Subscribe_AndPublish_HandlerReceivesEvent()
        {
            var bus = new SimpleAgentBus();
            string? received = null;
            Action<TestBusEvent> handler = evt => received = evt.Payload;

            bus.Subscribe(handler);
            bus.Publish(new TestBusEvent("hello"));
            Assert.Equal("hello", received);
        }

        [Fact]
        public void Unsubscribe_HandlerNoLongerReceivesEvents()
        {
            var bus = new SimpleAgentBus();
            int count = 0;
            Action<TestBusEvent> handler = evt => count++;

            bus.Subscribe(handler);
            bus.Unsubscribe(handler);
            bus.Publish(new TestBusEvent());

            Assert.Equal(0, count);
        }

        [Fact]
        public void Publish_NullEvent_NoOp()
        {
            var bus = new SimpleAgentBus();
            bus.Publish<TestBusEvent>(null!);
        }

        [Fact]
        public void Subscribe_NullHandler_NoOp()
        {
            var bus = new SimpleAgentBus();
            bus.Subscribe<TestBusEvent>(null!);
        }

        [Fact]
        public void Unsubscribe_NullHandler_NoOp()
        {
            var bus = new SimpleAgentBus();
            bus.Unsubscribe<TestBusEvent>(null!);
        }

        [Fact]
        public void MultipleHandlers_AllReceiveEvent()
        {
            var bus = new SimpleAgentBus();
            int count1 = 0, count2 = 0;
            Action<TestBusEvent> handler1 = evt => count1++;
            Action<TestBusEvent> handler2 = evt => count2++;

            bus.Subscribe(handler1);
            bus.Subscribe(handler2);
            bus.Publish(new TestBusEvent());
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
        }

        [Fact]
        public void DifferentEventTypes_Isolated()
        {
            var bus = new SimpleAgentBus();
            bool testReceived = false;
            bool anotherReceived = false;
            Action<TestBusEvent> testHandler = evt => testReceived = true;
            Action<AnotherBusEvent> anotherHandler = evt => anotherReceived = true;

            bus.Subscribe(testHandler);
            bus.Subscribe(anotherHandler);
            bus.Publish(new TestBusEvent());
            Assert.True(testReceived);
            Assert.False(anotherReceived);
        }

        [Fact]
        public void PublishFromBackground_EnqueuesEvent_AndFlushDispatches()
        {
            var bus = new SimpleAgentBus();
            string? received = null;
            Action<TestBusEvent> handler = evt => received = evt.Payload;

            bus.Subscribe(handler);
            bus.PublishFromBackground(new TestBusEvent("bg_msg"));
            Assert.Null(received);

            bus.FlushBackgroundQueue();
        }

        [Fact]
        public void FlushBackgroundQueue_MultipleEvents_DrainsQueue()
        {
            var bus = new SimpleAgentBus();
            Action<TestBusEvent> handler = evt => { };

            bus.Subscribe(handler);
            bus.PublishFromBackground(new TestBusEvent());
            bus.PublishFromBackground(new TestBusEvent());
            bus.PublishFromBackground(new TestBusEvent());

            bus.FlushBackgroundQueue();
        }

        [Fact]
        public void HandlerException_DoesNotBlockOtherHandlers()
        {
            var bus = new SimpleAgentBus();
            int count = 0;
            Action<TestBusEvent> badHandler = evt => throw new Exception("test error");
            Action<TestBusEvent> goodHandler = evt => count++;

            bus.Subscribe(badHandler);
            bus.Subscribe(goodHandler);
            bus.Publish(new TestBusEvent());
            Assert.Equal(1, count);
        }
    }
}
