using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Models.Agent;
using RimMind.Domain.Events;
using RimMind.Presentation.Agent;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Infrastructure.Verse;
using Verse;
using Verse.AI;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class MentalStateRecoveryTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly List<AgentLifecycleEvent> _capturedEvents = new();
        private readonly NpcManager _npcManager;
        private readonly IAgentBus _agentBus;
        private readonly string _capturedEventsSubscriptionKey;

        public MentalStateRecoveryTests()
        {
            _pawn = new Pawn { thingIDNumber = 77, Dead = false };
            _pawn.jobs = new Pawn_JobTracker { jobQueue = new JobQueue() };
            RimMindServiceLocator.Reset();
            _npcManager = new NpcManager(new Game());
            _npcManager.IndexPawn(_pawn);
            RimMindCoreMod.Settings = new RimMindCoreSettings
            {
                Context = new ContextSettings(),
                maxTokens = 800,
                defaultTemperature = 0.7f,
                thinkCooldownTicks = 30000,
                agentTickInterval = 150,
                maxToolCallDepth = 3,
                behaviorHistoryMax = 100,
            };
            _agentBus = new AgentBusImpl();
            _agent = new PawnAgent(_pawn, _agentBus);
            _agent.TransitionTo(AgentState.Active);

            _capturedEventsSubscriptionKey = _agentBus.Subscribe<AgentLifecycleEvent>(
                evt => _capturedEvents.Add(evt));
        }

        public void Dispose()
        {
            _agentBus.Unsubscribe<AgentLifecycleEvent>(_capturedEventsSubscriptionKey);
            if (_agent.State != AgentState.Terminated)
                _agent.TransitionTo(AgentState.Terminated);
            RimMindCoreMod.Settings = null;
            _npcManager.ClearPawnIndex();
        }

        [Fact]
        public void Tick_WhenPawnInMentalState_DoesNotThrow()
        {
            _pawn.InMentalState = true;
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WhenPawnRecoversFromMentalState_DoesNotThrow()
        {
            _pawn.InMentalState = true;
            _agent.Tick();

            _pawn.InMentalState = false;
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WhenPawnWasNeverInMentalState_DoesNotThrow()
        {
            _pawn.InMentalState = false;
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WhenPawnStillInMentalState_DoesNotThrow()
        {
            _pawn.InMentalState = true;
            var exception1 = Record.Exception(() => _agent.Tick());
            var exception2 = Record.Exception(() => _agent.Tick());
            Assert.Null(exception1);
            Assert.Null(exception2);
        }

        [Fact]
        public void AgentBus_SubscribeAndPublish_LifecycleEvent()
        {
            var received = new List<AgentLifecycleEvent>();
            _agentBus.Subscribe<AgentLifecycleEvent>(evt => received.Add(evt));

            _agentBus.Publish(new AgentLifecycleEvent("NPC-77", 77, "Active", "Paused"));

            Assert.Single(received);
            Assert.Equal("Active", received[0].PreviousState);
            Assert.Equal("Paused", received[0].NewState);
            Assert.Equal(77, received[0].PawnId);
        }

        [Fact]
        public void AgentBus_Unsubscribe_StopsReceivingEvents()
        {
            var received = new List<AgentLifecycleEvent>();
            Action<AgentLifecycleEvent> handler = evt => received.Add(evt);
            var key = _agentBus.Subscribe(handler);

            _agentBus.Publish(new AgentLifecycleEvent("NPC-77", 77, "Active", "Paused"));
            Assert.Single(received);

            _agentBus.Unsubscribe<AgentLifecycleEvent>(key);
            _agentBus.Publish(new AgentLifecycleEvent("NPC-77", 77, "Paused", "Active"));
            Assert.Single(received);
        }
    }
}
