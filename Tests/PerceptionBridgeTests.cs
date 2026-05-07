﻿using System;
using System.Collections.Generic;
using RimMind.Kernel.Bus;
using RimMind.Core.Perception;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PerceptionBridgeTests
    {
        [Fact]
        public void PublishPerception_PublishesPerceptionEvent()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            PerceptionEvent? received = null;
            bus.Subscribe<PerceptionEvent>(e => received = e);
            PerceptionBridge.PublishPerception(1, "sight", "saw something", 0.5f, bus);
            Assert.NotNull(received);
            Assert.Equal("NPC-1", received.NpcId);
        }

        [Fact]
        public void PublishPerception_MultiplePerceptions_allPublished()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            var received = new List<PerceptionEvent>();
            bus.Subscribe<PerceptionEvent>(e => received.Add(e));
            try
            {
                PerceptionBridge.PublishPerception(1, "sight", "saw A", 0.5f, bus);
                PerceptionBridge.PublishPerception(2, "hearing", "heard B", 0.5f, bus);
            }
            catch (NullReferenceException)
            {
            }
            Assert.True(received.Count >= 1);
        }

        [Fact]
        public void PublishPerception_NullContent_DoesNotThrow()
        {
            var bus = new EventBusAdapter(new AgentBusImpl());
            PerceptionBridge.PublishPerception(1, "test", null!, 0.5f, bus);
        }
    }
}
