using System;
using RimMind.Domain.Events;
using Xunit;

namespace RimMind.Tests.Application.Stubs
{
    public class StubAgentBusTests
    {
        [Fact]
        public void Unsubscribe_ByKey_ShouldRemoveHandler()
        {
            var bus = new StubAgentBus();
            var received = 0;

            var key = bus.Subscribe<PerceptionEvent>(_ => received++);
            bus.Publish(new PerceptionEvent("npc-1", 1, "sight", "first"));

            bus.Unsubscribe<PerceptionEvent>(key);
            bus.Publish(new PerceptionEvent("npc-1", 1, "sight", "second"));

            Assert.Equal(1, received);
        }

        [Fact]
        public void Unsubscribe_ByAction_ShouldThrow_NotSupported()
        {
            var bus = new StubAgentBus();
            Action<PerceptionEvent> handler = _ => { };
            bus.Subscribe("test-subscription", handler);

#pragma warning disable CS0618
            var exception = Assert.Throws<NotSupportedException>(() => bus.Unsubscribe(handler));
#pragma warning restore CS0618

            Assert.Contains("Use Unsubscribe<T>(string key)", exception.Message);
        }
    }
}
