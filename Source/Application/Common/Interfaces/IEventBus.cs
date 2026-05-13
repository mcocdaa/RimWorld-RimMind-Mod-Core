using System;
using RimMind.Domain.Events;

namespace RimMind.Application.Common.Interfaces
{
    public interface IEventBus
    {
        void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent;
        string Subscribe<T>(Action<T> handler) where T : AgentBusEvent;
        void Unsubscribe<T>(string key) where T : AgentBusEvent;
        void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent;
        void Publish<T>(T evt) where T : AgentBusEvent;
        void PublishFromBackground<T>(T evt) where T : AgentBusEvent;
        void FlushBackgroundQueue();
        void ClearAllSubscribers();
        int GetHandlerCount();
        int GetBackgroundQueueCount();
    }
}
