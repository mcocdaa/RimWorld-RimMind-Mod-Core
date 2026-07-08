using System;
using System.Collections.Generic;
using FluentAssertions;
using RimMind.Application.Features.AgentBus;
using RimMind.Domain.Events;

namespace RimMind.IntegrationTests.AgentBus
{
    public class AgentBusUnsubscribeTests
    {
        [Fact]
        public void Unsubscribe_ByKey_ShouldRemoveHandler()
        {
            var bus = new AgentBusImpl();
            var received = new List<PerceptionEvent>();

            var key = bus.Subscribe<PerceptionEvent>(evt => received.Add(evt));
            bus.Publish(new PerceptionEvent("npc-1", 1, "sight", "first"));

            bus.Unsubscribe<PerceptionEvent>(key);
            bus.Publish(new PerceptionEvent("npc-1", 1, "sight", "second"));

            received.Should().ContainSingle();
            received[0].Content.Should().Be("first");
        }

        [Fact]
        public void Unsubscribe_ByAction_ShouldThrow_NotSupported()
        {
            var bus = new AgentBusImpl();
            Action<PerceptionEvent> handler = _ => { };
            bus.Subscribe("test-subscription", handler);

#pragma warning disable CS0618
            Action act = () => bus.Unsubscribe<PerceptionEvent>(handler);
#pragma warning restore CS0618

            act.Should()
                .Throw<NotSupportedException>()
                .WithMessage("*Use Unsubscribe<T>(string key)*");
        }
    }
}
