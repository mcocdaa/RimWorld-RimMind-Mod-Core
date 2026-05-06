using System;
using RimMind.Contracts;
using RimMind.Core.AgentBus;

namespace RimMind.Core.AgentBus
{
    public class EventBusAdapter : IEventBus
    {
        private readonly IAgentBus _bus;

        public EventBusAdapter(IAgentBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent
            => _bus.Subscribe(key, handler);

        public string Subscribe<T>(Action<T> handler) where T : AgentBusEvent
            => _bus.Subscribe(handler);

        public void Unsubscribe<T>(string key) where T : AgentBusEvent
            => _bus.Unsubscribe<T>(key);

        public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent
            => _bus.Unsubscribe(handler);

        public void Publish<T>(T evt) where T : AgentBusEvent
            => _bus.Publish(evt);

        public void PublishFromBackground<T>(T evt) where T : AgentBusEvent
            => _bus.PublishFromBackground(evt);

        public void FlushBackgroundQueue()
            => _bus.FlushBackgroundQueue();

        public void ClearAllSubscribers()
            => _bus.ClearAllSubscribers();

        public int GetHandlerCount() => (_bus as AgentBusImpl)?.GetHandlerCount() ?? 0;
        public int GetBackgroundQueueCount() => (_bus as AgentBusImpl)?.GetBackgroundQueueCount() ?? 0;
    }
}
