using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Domain.Enums;
using RimMind.Presentation.Runtime;
using Verse;
using Xunit;

namespace RimMind.Tests.Presentation.Runtime
{
    [CollectionDefinition("RuntimeGameComponent", DisableParallelization = true)]
    public sealed class RuntimeGameComponentCollection { }

    [Collection("RuntimeGameComponent")]
    public sealed class RimMindRuntimeGameComponentTests : IDisposable
    {
        public RimMindRuntimeGameComponentTests()
        {
            RimMindServiceLocator.Reset();
            RimMindRuntime.ResetForTests();
            Find.TickManager = new TickManager();
        }

        public void Dispose()
        {
            RimMindServiceLocator.Reset();
            RimMindRuntime.ResetForTests();
        }

        [Fact]
        public void StartedNewGame_RepeatedCallsClearInOrderAndResetTickDeduplication()
        {
            AssertRepeatedLifecycleReset(component => component.StartedNewGame());
        }

        [Fact]
        public void LoadedGame_RepeatedCallsClearInOrderAndResetTickDeduplication()
        {
            AssertRepeatedLifecycleReset(component => component.LoadedGame());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Lifecycle_WhenScopedClearThrows_StillClearsScheduler(bool loadedGame)
        {
            var events = new List<string>();
            var scheduler = new RecordingScheduler(events);
            var manager = new RecordingScopedAgentManager(events) { ClearException = new InvalidOperationException("boom") };
            RegisterServices(scheduler, manager);
            var component = new RimMindRuntimeGameComponent(new Game());

            if (loadedGame) component.LoadedGame();
            else component.StartedNewGame();

            Assert.Equal(new[] { "scoped.clear", "scheduler.clear" }, events);
            Assert.Equal(1, scheduler.ClearCount);
        }

        [Fact]
        public void ServiceReplacement_RefreshesTickAndLifecycleTargets()
        {
            var eventsA = new List<string>();
            var schedulerA = new RecordingScheduler(eventsA);
            var managerA = new RecordingScopedAgentManager(eventsA);
            RegisterServices(schedulerA, managerA);
            var component = new RimMindRuntimeGameComponent(new Game());
            Find.TickManager.TicksGame = 10;
            component.GameComponentTick();

            var eventsB = new List<string>();
            var schedulerB = new RecordingScheduler(eventsB);
            var managerB = new RecordingScopedAgentManager(eventsB);
            RegisterServices(schedulerB, managerB);
            Find.TickManager.TicksGame = 11;
            component.GameComponentTick();
            component.StartedNewGame();

            Assert.Equal(new[] { 10 }, schedulerA.TickCalls);
            Assert.Equal(0, schedulerA.ClearCount);
            Assert.Equal(0, managerA.ClearCount);
            Assert.Equal(new[] { 11 }, schedulerB.TickCalls);
            Assert.Equal(new[] { "scoped.clear", "scheduler.clear" }, eventsB);
            Assert.Equal(1, RimMindRuntime.InitializeCallCount);
        }

        private static void AssertRepeatedLifecycleReset(Action<RimMindRuntimeGameComponent> lifecycle)
        {
            var events = new List<string>();
            var scheduler = new RecordingScheduler(events);
            var manager = new RecordingScopedAgentManager(events);
            RegisterServices(scheduler, manager);
            var component = new RimMindRuntimeGameComponent(new Game());
            Find.TickManager.TicksGame = 0;
            component.GameComponentTick();
            component.GameComponentTick();

            lifecycle(component);
            lifecycle(component);
            component.GameComponentTick();

            Assert.Equal(new[] { "scoped.clear", "scheduler.clear", "scoped.clear", "scheduler.clear" }, events);
            Assert.Equal(new[] { 0, 0 }, scheduler.TickCalls);
            Assert.Equal(1, RimMindRuntime.InitializeCallCount);
        }

        private static void RegisterServices(IAgentLoopScheduler scheduler, IScopedAgentManager manager)
        {
            RimMindServiceLocator.Register(scheduler);
            RimMindServiceLocator.Register(manager);
        }

        private sealed class RecordingScheduler : IAgentLoopScheduler
        {
            private readonly List<string> _events;

            public RecordingScheduler(List<string> events) => _events = events;

            public long Generation { get; private set; }
            public int ClearCount { get; private set; }
            public List<int> TickCalls { get; } = new();

            public bool Register(string key, AgentLoopKind kind, IAgentControl agent) => true;
            public bool Unregister(string key) => true;
            public IAgentControl? Find(string key) => null;
            public void Tick(int currentTick) => TickCalls.Add(currentTick);
            public void Clear()
            {
                _events.Add("scheduler.clear");
                ClearCount++;
                Generation++;
            }
            public AgentLoopSnapshot GetSnapshot() => AgentLoopSnapshot.Empty;
        }

        private sealed class RecordingScopedAgentManager : IScopedAgentManager
        {
            private readonly List<string> _events;

            public RecordingScopedAgentManager(List<string> events) => _events = events;

            public int ClearCount { get; private set; }
            public Exception? ClearException { get; set; }

            public IScopedAgent GetOrCreate(AgentScope scope, IAgentBus agentBus) => throw new NotSupportedException();
            public IScopedAgent GetOrCreate(string scopeType, string scopeId, IAgentBus agentBus, int? mapId = null) => throw new NotSupportedException();
            public IScopedAgent? Find(AgentScope scope) => null;
            public IScopedAgent? Find(string scopeType, string scopeId) => null;
            public IReadOnlyList<IScopedAgent> GetAll() => Array.Empty<IScopedAgent>();
            public bool Remove(AgentScope scope) => false;
            public bool Remove(string scopeType, string scopeId) => false;
            public void Clear()
            {
                _events.Add("scoped.clear");
                ClearCount++;
                if (ClearException != null) throw ClearException;
            }
        }
    }
}
