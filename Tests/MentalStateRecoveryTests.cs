﻿using System;
using System.Collections.Generic;
using RimMind.Contracts;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using RimMind.Core.Client;
using RimMind.Kernel.Flywheel;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using RimMind.Core.Settings;
using Verse;
using Verse.AI;
using Xunit;

namespace RimMind.Core.Tests
{
    public class MentalStateRecoveryTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly List<AgentLifecycleEvent> _capturedEvents = new();
        private readonly NpcManager _npcManager;
        private readonly IEventBus _eventBus;

        public MentalStateRecoveryTests()
        {
            _pawn = new Pawn { thingIDNumber = 77, Dead = false };
            _pawn.jobs = new Pawn_JobTracker { jobQueue = new JobQueue() };
            RimMindServiceLocator.Reset();
            _npcManager = new NpcManager(new Game());
            _npcManager.IndexPawn(_pawn);
            RimMindCoreMod.Settings = new AICoreSettings
            {
                Context = new ContextSettings(),
                maxTokens = 800,
                defaultTemperature = 0.7f,
                thinkCooldownTicks = 30000,
                agentTickInterval = 150,
                maxToolCallDepth = 3,
                behaviorHistoryMax = 100,
            };
            var flywheel = new FlywheelParameterStore();
            flywheel.FinalizeInit();
            _eventBus = new EventBusAdapter(new AgentBusImpl());
            _agent = new PawnAgent(_pawn, _eventBus);
            _agent.TransitionTo(AgentState.Active);

            _eventBus.Subscribe<AgentLifecycleEvent>(
                "MentalStateRecoveryTest",
                evt => _capturedEvents.Add(evt));
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<AgentLifecycleEvent>("MentalStateRecoveryTest");
            if (_agent.State != AgentState.Terminated)
                _agent.TransitionTo(AgentState.Terminated);
            RimMindCoreMod.Settings = null;
            _npcManager.ClearPawnIndex();
        }

        [Fact]
        public void Tick_WhenPawnRecoversFromMentalState_PublishesLifecycleEvent()
        {
            _pawn.InMentalState = true;
            _agent.Tick();

            _pawn.InMentalState = false;
            _agent.Tick();

            Assert.Contains(_capturedEvents, e =>
                e.NewState == "MentalStateRecovered" &&
                e.PawnId == 77);
        }

        [Fact]
        public void Tick_WhenPawnRecoversFromMentalState_EventHasPreviousStateMentalBreak()
        {
            _pawn.InMentalState = true;
            _agent.Tick();

            _pawn.InMentalState = false;
            _agent.Tick();

            Assert.Contains(_capturedEvents, e =>
                e.PreviousState == "MentalBreak" &&
                e.NewState == "MentalStateRecovered");
        }

        [Fact]
        public void Tick_WhenPawnWasNeverInMentalState_NoRecoveryEvent()
        {
            _pawn.InMentalState = false;
            _agent.Tick();

            Assert.DoesNotContain(_capturedEvents, e =>
                e.NewState == "MentalStateRecovered");
        }

        [Fact]
        public void Tick_WhenPawnStillInMentalState_NoRecoveryEvent()
        {
            _pawn.InMentalState = true;
            _agent.Tick();
            _agent.Tick();

            Assert.DoesNotContain(_capturedEvents, e =>
                e.NewState == "MentalStateRecovered");
        }

        [Fact]
        public void Tick_MentalStateRecovery_ResumesThinkCycle()
        {
            _pawn.InMentalState = true;
            _agent.Tick();

            _pawn.InMentalState = false;
            _agent.Tick();

            Assert.Contains(_capturedEvents, e =>
                e.EventType == AgentBusEventType.Lifecycle);
        }
    }
}
