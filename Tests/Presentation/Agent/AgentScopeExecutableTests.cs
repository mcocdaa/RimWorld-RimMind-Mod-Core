using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Features.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using RimMind.Presentation.Agent;
using Xunit;

namespace RimMind.Tests.Presentation.Agent
{
    public class AgentScopeExecutableTests
    {
        [Fact]
        public void AgentScope_Pawn_CompositeKey_Differs_By_MapId()
        {
            var firstMapScope = AgentScope.Pawn("pawn-42", mapId: 1);
            var secondMapScope = AgentScope.Pawn("pawn-42", mapId: 2);

            Assert.NotEqual(firstMapScope.CompositeKey, secondMapScope.CompositeKey);
        }

        [Fact]
        public void Legacy_Manager_Overloads_Find_And_Remove_Agent_Created_With_MapId()
        {
            var scheduler = new AgentLoopScheduler();
            var manager = new ScopedAgentManager(new ScopedAgentFactory(), scheduler);
            var bus = new AgentBusImpl();
            var legacyManagerKey = "storyteller:x";

            var created = manager.GetOrCreate("storyteller", "x", bus, mapId: 1);
            var found = manager.Find("storyteller", "x");
            var registered = scheduler.Find(AgentLoopKeys.ForScoped(legacyManagerKey));
            var removed = manager.Remove("storyteller", "x");

            Assert.Same(created, found);
            Assert.Same(created, registered);
            Assert.True(removed);
            Assert.Null(manager.Find("storyteller", "x"));
            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(legacyManagerKey)));
            Assert.Equal(AgentState.Terminated, created.State);
        }

        [Fact]
        public void GetOrCreate_Scope_Registers_Agent_And_Remove_Unregisters_Then_Terminates()
        {
            var scheduler = new AgentLoopScheduler();
            var manager = new ScopedAgentManager(new ScopedAgentFactory(), scheduler);
            var bus = new AgentBusImpl();
            var scope = AgentScope.Map(7);
            var loopKey = AgentLoopKeys.ForScoped(scope.CompositeKey);

            var agent = manager.GetOrCreate(scope, bus);

            Assert.Same(agent, scheduler.Find(loopKey));

            Assert.True(manager.Remove(scope));
            Assert.Null(scheduler.Find(loopKey));
            Assert.Equal(AgentState.Terminated, agent.State);
        }

        [Fact]
        public void Clear_Unregisters_And_Terminates_All_Scoped_Agents()
        {
            var scheduler = new AgentLoopScheduler();
            var manager = new ScopedAgentManager(new ScopedAgentFactory(), scheduler);
            var bus = new AgentBusImpl();
            var mapScope = AgentScope.Map(7);
            var globalScope = AgentScope.Global("colony");
            var mapAgent = manager.GetOrCreate(mapScope, bus);
            var globalAgent = manager.GetOrCreate(globalScope, bus);

            manager.Clear();

            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(mapScope.CompositeKey)));
            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(globalScope.CompositeKey)));
            Assert.Equal(AgentState.Terminated, mapAgent.State);
            Assert.Equal(AgentState.Terminated, globalAgent.State);
        }

        [Fact]
        public void GetOrCreate_When_Register_Throws_Does_Not_Cache_And_Allows_Retry()
        {
            var firstAgent = new LifecycleAgent();
            var retryAgent = new LifecycleAgent();
            var factory = new QueueScopedAgentFactory(firstAgent, retryAgent);
            var scheduler = new RecordingScheduler { RegisterFailuresRemaining = 1 };
            var manager = new ScopedAgentManager(factory, scheduler);
            var bus = new AgentBusImpl();
            var scope = AgentScope.Global("retry");

            Assert.Throws<InvalidOperationException>(() => manager.GetOrCreate(scope, bus));
            Assert.Null(manager.Find(scope));

            var created = manager.GetOrCreate(scope, bus);

            Assert.Same(retryAgent, created);
            Assert.Equal(2, factory.CreateCount);
            Assert.Same(retryAgent, scheduler.Find(AgentLoopKeys.ForScoped(scope.CompositeKey)));
        }

        [Fact]
        public void Remove_Unregisters_Before_Lifecycle_And_Aggregates_Cleanup_Failure()
        {
            var events = new List<string>();
            var cleanupError = new InvalidOperationException("cleanup failed");
            var agent = new LifecycleAgent(events, cleanupError);
            var factory = new QueueScopedAgentFactory(agent);
            var scheduler = new RecordingScheduler(events);
            var manager = new ScopedAgentManager(factory, scheduler);
            var bus = new AgentBusImpl();
            var scope = AgentScope.Global("remove-failure");
            var loopKey = AgentLoopKeys.ForScoped(scope.CompositeKey);
            manager.GetOrCreate(scope, bus);

            var error = Assert.Throws<AggregateException>(() => manager.Remove(scope));

            Assert.Collection(error.InnerExceptions, exception => Assert.Same(cleanupError, exception));
            Assert.Equal(1, agent.CleanupCount);
            Assert.Equal(1, agent.DestroyCount);
            Assert.Null(manager.Find(scope));
            Assert.Null(scheduler.Find(loopKey));
            Assert.Equal(
                new[] { $"register:{loopKey}", $"unregister:{loopKey}", "cleanup", "destroy" },
                events);
        }

        [Fact]
        public void Clear_Continues_After_Lifecycle_Failures_And_Empties_Manager_And_Scheduler()
        {
            var cleanupError = new InvalidOperationException("cleanup failed");
            var destroyError = new InvalidOperationException("destroy failed");
            var throwingAgent = new LifecycleAgent(cleanupError: cleanupError, destroyError: destroyError);
            var healthyAgent = new LifecycleAgent();
            var factory = new QueueScopedAgentFactory(throwingAgent, healthyAgent);
            var scheduler = new RecordingScheduler();
            var manager = new ScopedAgentManager(factory, scheduler);
            var bus = new AgentBusImpl();
            var throwingScope = AgentScope.Global("throwing");
            var healthyScope = AgentScope.Global("healthy");
            manager.GetOrCreate(throwingScope, bus);
            manager.GetOrCreate(healthyScope, bus);

            var error = Assert.Throws<AggregateException>(() => manager.Clear());

            Assert.Collection(
                error.InnerExceptions,
                exception => Assert.Same(cleanupError, exception),
                exception => Assert.Same(destroyError, exception));
            Assert.Equal(1, throwingAgent.CleanupCount);
            Assert.Equal(1, throwingAgent.DestroyCount);
            Assert.Equal(1, healthyAgent.CleanupCount);
            Assert.Equal(1, healthyAgent.DestroyCount);
            Assert.Empty(manager.GetAll());
            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(throwingScope.CompositeKey)));
            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(healthyScope.CompositeKey)));
        }

        [Fact]
        public void Clear_When_Unregister_Throws_Keeps_That_Manager_Entry_And_Continues()
        {
            var unregisterError = new InvalidOperationException("unregister failed");
            var retainedAgent = new LifecycleAgent();
            var removedAgent = new LifecycleAgent();
            var factory = new QueueScopedAgentFactory(retainedAgent, removedAgent);
            var scheduler = new RecordingScheduler();
            var manager = new ScopedAgentManager(factory, scheduler);
            var bus = new AgentBusImpl();
            var retainedScope = AgentScope.Global("retained");
            var removedScope = AgentScope.Global("removed");
            var retainedLoopKey = AgentLoopKeys.ForScoped(retainedScope.CompositeKey);
            var removedLoopKey = AgentLoopKeys.ForScoped(removedScope.CompositeKey);
            manager.GetOrCreate(retainedScope, bus);
            manager.GetOrCreate(removedScope, bus);
            scheduler.UnregisterFailures.Add(retainedLoopKey, unregisterError);

            var error = Assert.Throws<AggregateException>(() => manager.Clear());

            Assert.Collection(error.InnerExceptions, exception => Assert.Same(unregisterError, exception));
            Assert.Same(retainedAgent, manager.Find(retainedScope));
            Assert.Same(retainedAgent, scheduler.Find(retainedLoopKey));
            Assert.Equal(0, retainedAgent.CleanupCount);
            Assert.Equal(0, retainedAgent.DestroyCount);
            Assert.Null(manager.Find(removedScope));
            Assert.Null(scheduler.Find(removedLoopKey));
            Assert.Equal(1, removedAgent.CleanupCount);
            Assert.Equal(1, removedAgent.DestroyCount);
        }

        [Fact]
        public void Constructor_Rejects_Null_Dependencies()
        {
            var factory = new ScopedAgentFactory();
            var scheduler = new AgentLoopScheduler();

            Assert.Throws<ArgumentNullException>(() => new ScopedAgentManager(null!, scheduler));
            Assert.Throws<ArgumentNullException>(() => new ScopedAgentManager(factory, null!));
        }

        [Fact]
        public void Legacy_Created_ScopedAgent_Preserves_Caller_Provided_ScopeType()
        {
            var factory = new ScopedAgentFactory();
            var bus = new AgentBusImpl();

            var agent = factory.Create("storyteller", "x", bus, mapId: 1);

            Assert.Equal("storyteller", agent.ScopeType);
            Assert.Equal("x", agent.ScopeId);
            Assert.Equal(1, agent.MapId);
        }

        private sealed class QueueScopedAgentFactory : IScopedAgentFactory
        {
            private readonly Queue<IScopedAgent> _agents;

            public QueueScopedAgentFactory(params IScopedAgent[] agents)
            {
                _agents = new Queue<IScopedAgent>(agents);
            }

            public int CreateCount { get; private set; }

            public IScopedAgent Create(AgentScope scope, IAgentBus agentBus)
            {
                CreateCount++;
                return _agents.Dequeue();
            }

            public IScopedAgent Create(string scopeType, string scopeId, IAgentBus agentBus, int? mapId = null)
                => Create(AgentScope.Custom(scopeType, scopeId, mapId), agentBus);
        }

        private sealed class RecordingScheduler : IAgentLoopScheduler
        {
            private readonly Dictionary<string, IAgentControl> _agents = new();
            private readonly List<string>? _events;

            public RecordingScheduler(List<string>? events = null)
            {
                _events = events;
            }

            public int RegisterFailuresRemaining { get; set; }
            public Dictionary<string, Exception> UnregisterFailures { get; } = new();

            public bool Register(string key, AgentLoopKind kind, IAgentControl agent)
            {
                _events?.Add($"register:{key}");
                if (RegisterFailuresRemaining > 0)
                {
                    RegisterFailuresRemaining--;
                    throw new InvalidOperationException("register failed");
                }

                _agents[key] = agent;
                return true;
            }

            public bool Unregister(string key)
            {
                _events?.Add($"unregister:{key}");
                if (UnregisterFailures.TryGetValue(key, out var error))
                    throw error;
                return _agents.Remove(key);
            }

            public IAgentControl? Find(string key)
                => _agents.TryGetValue(key, out var agent) ? agent : null;

            public void Tick(int currentTick)
            {
            }

            public void Clear() => _agents.Clear();

            public AgentLoopSnapshot GetSnapshot() => AgentLoopSnapshot.Empty;
        }

        private sealed class LifecycleAgent : IScopedAgent
        {
            private readonly List<string>? _events;
            private readonly Exception? _cleanupError;
            private readonly Exception? _destroyError;
            private AgentState _state = AgentState.Dormant;

            public LifecycleAgent(
                List<string>? events = null,
                Exception? cleanupError = null,
                Exception? destroyError = null)
            {
                _events = events;
                _cleanupError = cleanupError;
                _destroyError = destroyError;
            }

            public string ScopeId => "stub";
            public string ScopeType => "stub";
            public int? MapId => null;
            public AgentState State => _state;
            public bool IsActive => _state == AgentState.Active;
            public AgentModeId CurrentModeId => AgentModeId.Dormant;
            public IAgentMode CurrentMode => null!;
            public bool IsPawnValid => false;
            public string NpcId => ScopeId;
            public string Label => "Lifecycle agent";
            public int? LastThinkTick { get; set; }
            public int GoalCount => 0;
            public int CleanupCount { get; private set; }
            public int DestroyCount { get; private set; }

            public void Tick()
            {
            }

            public bool TransitionTo(AgentState newState)
            {
                _state = newState;
                return true;
            }

            public void ForceThink()
            {
            }

            public void SwitchMode(AgentModeId modeId)
            {
            }

            public void Cleanup()
            {
                CleanupCount++;
                _events?.Add("cleanup");
                if (_cleanupError != null)
                    throw _cleanupError;
                _state = AgentState.Terminated;
            }

            public void Destroy()
            {
                DestroyCount++;
                _events?.Add("destroy");
                if (_destroyError != null)
                    throw _destroyError;
                _state = AgentState.Terminated;
            }

            public void ResubscribeEvents()
            {
            }

            public bool RemoveGoal(string goalDescription) => false;

            public void RecordBehavior(BehaviorRecordDto record)
            {
            }

            public IReadOnlyList<BehaviorRecordDto> GetRecentHistory(int count = 10)
                => Array.Empty<BehaviorRecordDto>();

            public float GetRecentSuccessRate(int count = 10) => 0f;

            public object? ConsumePendingJob() => null;

            public string GetDebugInfo() => string.Empty;
        }
    }
}
