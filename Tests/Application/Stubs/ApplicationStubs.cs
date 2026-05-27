using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Domain.Agent.Psychology;
using RimMind.Domain.Enums;
using RimMind.Domain.Events;

namespace RimMind.Tests.Application.Stubs
{
    /// <summary>
    /// Mutable tick provider stub for Application layer tests.
    /// </summary>
    internal sealed class StubTickProvider : ITickProvider
    {
        public int TicksGame { get; set; } = 0;
    }

    /// <summary>
    /// Agent bus stub that records published events and supports subscribe/unsubscribe.
    /// </summary>
    internal sealed class StubAgentBus : IAgentBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly Dictionary<string, Delegate> _subscriptions = new();
        private int _subKeyCounter;

        public List<AgentBusEvent> PublishedEvents { get; } = new();

        public void Publish<T>(T evt) where T : AgentBusEvent
        {
            PublishedEvents.Add(evt);
            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                foreach (var handler in list.ToList())
                {
                    ((Action<T>)handler)(evt);
                }
            }
        }

        public void PublishFromBackground<T>(T evt) where T : AgentBusEvent
        {
            Publish(evt);
        }

        public string Subscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            var key = $"sub-{typeof(T).Name}-{++_subKeyCounter}";
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }
            list.Add(handler);
            _subscriptions[key] = handler;
            return key;
        }

        public void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }
            list.Add(handler);
            _subscriptions[key] = handler;
        }

        public void Unsubscribe<T>(string key) where T : AgentBusEvent
        {
            if (_subscriptions.TryGetValue(key, out var handler))
            {
                if (_handlers.TryGetValue(typeof(T), out var list))
                {
                    list.Remove(handler);
                }
                _subscriptions.Remove(key);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                list.Remove(handler);
            }
        }

        public string SubscribeByName(string eventTypeName, Action<AgentBusEvent> handler)
        {
            return $"byname-{eventTypeName}-{++_subKeyCounter}";
        }

        public bool HasSubscription<T>() where T : AgentBusEvent
        {
            return _handlers.TryGetValue(typeof(T), out var list) && list.Count > 0;
        }

        // IAgentBusAdministration
        public event Action? SubscribersCleared;
        public void SetPipeline(IPipeline<BusPublishContext> pipeline) { }
        public void FlushBackgroundQueue() { }
        public void ClearAllSubscribers()
        {
            _handlers.Clear();
            _subscriptions.Clear();
            SubscribersCleared?.Invoke();
        }
        public int GetHandlerCount() => _handlers.Values.Sum(l => l.Count);
        public int GetBackgroundQueueCount() => 0;
        public Action<AgentBusEvent>? DispatchAction => null;
    }

    /// <summary>
    /// Mutable agent info stub for Application layer tests.
    /// </summary>
    internal class StubAgentInfo : IAgentInfo
    {
        public string NpcId { get; set; } = "test-npc";
        public string Label { get; set; } = "Test";
        public AgentState State { get; set; } = AgentState.Active;
        public int? LastThinkTick { get; set; } = null;
        public int GoalCount { get; set; } = 0;
    }

    /// <summary>
    /// Sleep detector stub for DefaultDreamGenerator tests.
    /// </summary>
    internal sealed class StubSleepDetector : ISleepDetector
    {
        public bool Sleeping { get; set; } = false;
        public bool IsSleeping(IAgentInfo agent) => Sleeping;
    }

    /// <summary>
    /// Psychology data provider stub for DefaultPsychologyWatcher tests.
    /// </summary>
    internal sealed class StubPsychologyDataProvider : IPawnPsychologyDataProvider
    {
        public float MoodLevel { get; set; } = 0.7f;
        public float MentalBreakThreshold { get; set; } = 0.1f;
        public IReadOnlyList<NeedLevel> NeedLevels { get; set; } = Array.Empty<NeedLevel>();
        public bool InMentalState { get; set; } = false;

        public float GetMoodLevel(int pawnId) => MoodLevel;
        public IReadOnlyList<NeedLevel> GetNeedLevels(int pawnId) => NeedLevels;
        public float GetMentalBreakThreshold(int pawnId) => MentalBreakThreshold;
        public bool IsInMentalState(int pawnId) => InMentalState;
    }

    /// <summary>
    /// Minimal IPsychologyWatcher stub for DefaultTraitEvolutionEngine tests.
    /// </summary>
    internal sealed class StubPsychologyWatcher : IPsychologyWatcher
    {
        public void CheckAndPublish(IAgentInfo agent, int pawnId) { }
        public bool HasUrgentEvent(string npcId) => false;
    }
}
