using System;
using Verse;

namespace RimMind.Core.AgentBus
{
    public class EventBusAdapter : IEventBus
    {
        public void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent
            => AgentBus.Subscribe(key, handler);

        public string Subscribe<T>(Action<T> handler) where T : AgentBusEvent
            => AgentBus.Subscribe(handler);

        public void Unsubscribe<T>(string key) where T : AgentBusEvent
            => AgentBus.Unsubscribe<T>(key);

        public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent
            => AgentBus.Unsubscribe(handler);

        public void Publish<T>(T evt) where T : AgentBusEvent
            => AgentBus.Publish(evt);

        public void Publish(AgentBusEvent evt)
            => AgentBus.Publish(evt);

        public void PublishFromBackground<T>(T evt) where T : AgentBusEvent
            => AgentBus.PublishFromBackground(evt);

        public void FlushBackgroundQueue()
            => AgentBus.FlushBackgroundQueue();

        public void ClearAllSubscribers()
            => AgentBus.ClearAllSubscribers();
    }
}
