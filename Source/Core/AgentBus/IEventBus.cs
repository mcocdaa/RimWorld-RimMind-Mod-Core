using System;
using RimMind.Contracts;

namespace RimMind.Kernel.Bus
{
    public interface IEventBus
    {
        void Subscribe<T>(string key, Action<T> handler) where T : Contracts.AgentBusEvent;
        string Subscribe<T>(Action<T> handler) where T : Contracts.AgentBusEvent;
        void Unsubscribe<T>(string key) where T : Contracts.AgentBusEvent;
        void Unsubscribe<T>(Action<T> handler) where T : Contracts.AgentBusEvent;
        void Publish<T>(T evt) where T : Contracts.AgentBusEvent;
        void PublishFromBackground<T>(T evt) where T : Contracts.AgentBusEvent;
        void FlushBackgroundQueue();
        void ClearAllSubscribers();
        int GetHandlerCount();
        int GetBackgroundQueueCount();
    }
}
