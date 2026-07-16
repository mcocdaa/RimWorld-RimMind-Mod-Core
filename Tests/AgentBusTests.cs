using System;
using System.Collections.Generic;
using RimMind.Domain.Events;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Defaults;
using RimMind.Application.Features.Context;
using RimMind.Application.Common.Interfaces.Context;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class AgentBusTests
    {
        [Fact]
        public void AgentBusImpl_ImplementsIAgentBus()
        {
            IAgentBus bus = new AgentBusImpl();
            Assert.NotNull(bus);
        }

        [Fact]
        public void Subscribe_AndPublish_HandlerReceivesEvent()
        {
            var bus = new AgentBusImpl();
            DecisionEvent? received = null;
            bus.Subscribe<DecisionEvent>(e => received = e);
            bus.Publish(new DecisionEvent("test_npc", 0, "type", "reason", "action"));
            Assert.NotNull(received);
            Assert.Equal("test_npc", received.NpcId);
        }

        [Fact]
        public void Unsubscribe_HandlerNoLongerReceivesEvents()
        {
            var bus = new AgentBusImpl();
            int count = 0;
            Action<DecisionEvent> handler = e => count++;
            var key = bus.Subscribe(handler);
            bus.Publish(new DecisionEvent("n1", 0, "t", "r", "a"));
            Assert.Equal(1, count);
            bus.Unsubscribe<DecisionEvent>(key);
            bus.Publish(new DecisionEvent("n2", 0, "t", "r", "a"));
            Assert.Equal(1, count);
        }

        [Fact]
        public void MultipleHandlers_AllReceiveEvent()
        {
            var bus = new AgentBusImpl();
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
            var bus = new AgentBusImpl();
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
            var bus = new AgentBusImpl();
            int count = 0;
            bus.Subscribe<DecisionEvent>(e => count++);
            bus.ClearAllSubscribers();
            bus.Publish(new DecisionEvent("n", 0, "t", "r", "a"));
            Assert.Equal(0, count);
        }

        [Fact]
        public void AgentBusCoreSubscriber_Dispose_RemovesOnlyOwnedHandlers()
        {
            var bus = new AgentBusImpl();
            var thirdPartyCalls = 0;
            bus.Subscribe<GoalEvent>(_ => thirdPartyCalls++);
            var core = new AgentBusCoreSubscriber(bus, new SilentLogSink());

            Assert.Equal(7, bus.GetHandlerCount());

            core.Dispose();
            bus.Publish(new GoalEvent("npc", 1, "goal", "active", "test"));

            Assert.Equal(1, bus.GetHandlerCount());
            Assert.Equal(1, thirdPartyCalls);
        }

        [Fact]
        public void FlushBackgroundQueue_DoesNotThrow()
        {
            var bus = new AgentBusImpl();
            bus.FlushBackgroundQueue();
        }

        [Fact]
        public void PublishFromBackground_EnqueuesForFlush()
        {
            var bus = new AgentBusImpl();
            DecisionEvent? received = null;
            bus.Subscribe<DecisionEvent>(e => received = e);
            bus.PublishFromBackground(new DecisionEvent("bg", 0, "t", "r", "a"));
            Assert.Null(received);
            bus.FlushBackgroundQueue();
        }

        [Fact]
        public void HandlerException_DoesNotBlockOtherHandlers()
        {
            var bus = new AgentBusImpl();
            int count = 0;
            bus.Subscribe<DecisionEvent>(e => throw new Exception("test error"));
            bus.Subscribe<DecisionEvent>(e => count++);
            bus.Publish(new DecisionEvent("n", 0, "t", "r", "a"));
            Assert.Equal(1, count);
        }

        private sealed class SilentLogSink : ILogSink
        {
            public void Message(string msg) { }
            public void Warning(string msg) { }
            public void Error(string msg) { }
            public void LogFromBackground(string msg, bool isWarning = false) { }
        }
    }
}
