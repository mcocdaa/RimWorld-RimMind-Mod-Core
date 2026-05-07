﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using RimMind.Kernel.Bus;
using RimMind.Kernel.Context;
using Xunit;

namespace RimMind.Core.Tests
{
    public class AgentBusTests
    {
        [Fact]
        public void EventBusAdapter_ImplementsIEventBus()
        {
            IEventBus bus = new EventBusAdapter(new AgentBusImpl());
            Assert.NotNull(bus);
        }

        [Fact]
        public void Subscribe_AndPublish_HandlerReceivesEvent()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            DecisionEvent? received = null;
            bus.Subscribe<DecisionEvent>(e => received = e);
            bus.Publish(new DecisionEvent("test_npc", 0, "type", "reason", "action"));
            Assert.NotNull(received);
            Assert.Equal("test_npc", received.NpcId);
        }

        [Fact]
        public void Unsubscribe_HandlerNoLongerReceivesEvents()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            int count = 0;
            Action<DecisionEvent> handler = e => count++;
            bus.Subscribe(handler);
            bus.Publish(new DecisionEvent("n1", 0, "t", "r", "a"));
            Assert.Equal(1, count);
            bus.Unsubscribe(handler);
            bus.Publish(new DecisionEvent("n2", 0, "t", "r", "a"));
            Assert.Equal(1, count);
        }

        [Fact]
        public void MultipleHandlers_AllReceiveEvent()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            int count1 = 0, count2 = 0;
            bus.Subscribe<DecisionEvent>(e => count1++);
            bus.Subscribe<DecisionEvent>(e => count2++);
            bus.Publish(new DecisionEvent("n", 0, "t", "r", "a"));
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
        }

        [Fact]
        public void DifferentEventTypes_Isolated()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            bool decisionReceived = false;
            bool perceptionReceived = false;
            bus.Subscribe<DecisionEvent>(e => decisionReceived = true);
            bus.Subscribe<PerceptionEvent>(e => perceptionReceived = true);
            bus.Publish(new DecisionEvent("n", 0, "t", "r", "a"));
            Assert.True(decisionReceived);
            Assert.False(perceptionReceived);
        }

        [Fact]
        public void ClearAllSubscribers_NoHandlersReceiveEvents()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            int count = 0;
            bus.Subscribe<DecisionEvent>(e => count++);
            bus.ClearAllSubscribers();
            bus.Publish(new DecisionEvent("n", 0, "t", "r", "a"));
            Assert.Equal(0, count);
        }

        [Fact]
        public void FlushBackgroundQueue_DoesNotThrow()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            bus.FlushBackgroundQueue();
        }

        [Fact]
        public void PublishFromBackground_EnqueuesForFlush()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            DecisionEvent? received = null;
            bus.Subscribe<DecisionEvent>(e => received = e);
            bus.PublishFromBackground(new DecisionEvent("bg", 0, "t", "r", "a"));
            Assert.Null(received);
            bus.FlushBackgroundQueue();
        }

        [Fact]
        public void HandlerException_DoesNotBlockOtherHandlers()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            int count = 0;
            bus.Subscribe<DecisionEvent>(e => throw new Exception("test error"));
            bus.Subscribe<DecisionEvent>(e => count++);
            bus.Publish(new DecisionEvent("n", 0, "t", "r", "a"));
            Assert.Equal(1, count);
        }
    }
}
