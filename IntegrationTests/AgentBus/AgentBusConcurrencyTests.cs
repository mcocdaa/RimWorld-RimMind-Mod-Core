using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RimMind.Application.Features.AgentBus;
using RimMind.Domain.Events;

namespace RimMind.IntegrationTests.AgentBus
{
    /// <summary>
    /// Verifies thread-safety of <see cref="AgentBusImpl.RegisterEventType"/> and
    /// <see cref="AgentBusImpl.SubscribeByName"/> when invoked concurrently.
    /// Regression guard for the static EventTypeMap Dictionary → ConcurrentDictionary migration.
    /// </summary>
    public class AgentBusConcurrencyTests
    {
        [Fact]
        public void RegisterEventType_Concurrent_ShouldNotThrow()
        {
            var bus = new AgentBusImpl();
            var errors = new ConcurrentQueue<Exception>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    bus.RegisterEventType($"CustomEvent{i}", typeof(PerceptionEvent));
                }
                catch (Exception ex)
                {
                    errors.Enqueue(ex);
                }
            });

            errors.Should().BeEmpty("RegisterEventType should be thread-safe");
        }

        [Fact]
        public void RegisterEventType_And_SubscribeByName_Concurrent_ShouldNotThrow()
        {
            var bus = new AgentBusImpl();
            var errors = new ConcurrentQueue<Exception>();

            // Mix writers (RegisterEventType) and readers (SubscribeByName) on distinct keys
            // to stress the EventTypeMap concurrent read/write path.
            Parallel.Invoke(
                () =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        try
                        {
                            bus.RegisterEventType($"CustomEvent{i}", typeof(PerceptionEvent));
                        }
                        catch (Exception ex)
                        {
                            errors.Enqueue(ex);
                        }
                    }
                },
                () =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        try
                        {
                            // Some names are registered by the writer; some are not. Both paths
                            // exercise EventTypeMap.TryGetValue under concurrent mutation.
                            bus.SubscribeByName($"CustomEvent{i % 60}", _ => { });
                        }
                        catch (Exception ex)
                        {
                            errors.Enqueue(ex);
                        }
                    }
                });

            errors.Should().BeEmpty("EventTypeMap concurrent read/write should be thread-safe");
        }
    }
}
