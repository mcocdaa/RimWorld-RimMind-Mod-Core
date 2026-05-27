using System;
using RimMind.Domain.Common;
using RimMind.Domain.Events;

namespace RimMind.Application.Common.Interfaces
{
    public interface IAgentBus
    {
        event Action? SubscribersCleared;

        string Subscribe<T>(Action<T> handler) where T : AgentBusEvent;
        void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent;
        void Unsubscribe<T>(string key) where T : AgentBusEvent;
        void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent;

        /// <summary>
        /// Subscribe to events by AgentBusEventType name (string) instead of generic type.
        /// Returns a subscription key that can be used to unsubscribe.
        /// Used by ProviderCache for invalidation triggers where the event type is not known at compile time.
        /// </summary>
        string SubscribeByName(string eventTypeName, Action<AgentBusEvent> handler);

        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        void Publish<T>(T evt) where T : AgentBusEvent;

        [ThreadAffinity(ThreadAffinityKind.Any)]
        void PublishFromBackground<T>(T evt) where T : AgentBusEvent;

        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        void FlushBackgroundQueue();

        void ClearAllSubscribers();

        [ThreadAffinity(ThreadAffinityKind.Any)]
        int GetHandlerCount();

        [ThreadAffinity(ThreadAffinityKind.Any)]
        int GetBackgroundQueueCount();

        /// <summary>
        /// Delegate that dispatches an event to registered handlers.
        /// Used by BusPublishPipeline to re-dispatch after middleware processing.
        /// </summary>
        Action<AgentBusEvent>? DispatchAction { get; }
    }
}
