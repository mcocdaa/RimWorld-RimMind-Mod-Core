using System;
using RimMind.Core.AgentBus;

namespace RimMind.Contracts;

public interface IAgentBus
{
    string Subscribe<T>(Action<T> handler) where T : AgentBusEvent;
    void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent;
    void Unsubscribe<T>(string key) where T : AgentBusEvent;
    void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent;

    [ThreadAffinity(ThreadAffinityKind.MainOnly)]
    void Publish<T>(T evt) where T : AgentBusEvent;

    [ThreadAffinity(ThreadAffinityKind.Any)]
    void PublishFromBackground<T>(T evt) where T : AgentBusEvent;

    [ThreadAffinity(ThreadAffinityKind.MainOnly)]
    void FlushBackgroundQueue();

    void ClearAllSubscribers();
}
