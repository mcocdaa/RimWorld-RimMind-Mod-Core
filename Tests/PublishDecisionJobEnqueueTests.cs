using System;
using System.Collections.Generic;
using RimMind.Presentation;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Agent;
using RimMind.Infrastructure.Patches;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Common.Interfaces;
using RimMind.Infrastructure.Services.Clients;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Features.Flywheel;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Infrastructure.Verse;
using Verse;
using Verse.AI;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class PawnAgentJobTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly IFlywheelParameterStore? _originalFlywheel;
        private readonly IAgentActionBridge? _originalBridge;
        private readonly NpcManager _npcManager;

        public PawnAgentJobTests()
        {
            RegisterJobDefs();
            _pawn = new Pawn { thingIDNumber = 42, Dead = false };
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
            _originalFlywheel = FlywheelParameterStore.Instance;
            _agent = new PawnAgent(_pawn, new AgentBusImpl());
            _agent.TransitionTo(AgentState.Active);
            _originalBridge = RimMindAPI.GetAgentActionBridge();
        }

        public void Dispose()
        {
            if (_originalBridge != null)
                RimMindAPI.RegisterAgentActionBridge(_originalBridge);
            if (_agent.State != AgentState.Terminated)
                _agent.TransitionTo(AgentState.Terminated);
            RimMindCoreMod.Settings = null;
            if (_originalFlywheel != null)
                RimMindServiceLocator.Register<IFlywheelParameterStore>(_originalFlywheel);
            _npcManager.ClearPawnIndex();
            DefDatabase<JobDef>.Clear();
        }

        private static void RegisterJobDefs()
        {
            if (DefDatabase<JobDef>.AllDefsListForReading.Count > 0) return;
            DefDatabase<JobDef>.AddDef(new JobDef { defName = "RimMind_GenericAction" });
            DefDatabase<JobDef>.AddDef(new JobDef { defName = "RimMind_Rest" });
            DefDatabase<JobDef>.AddDef(new JobDef { defName = "RimMind_Work" });
            DefDatabase<JobDef>.AddDef(new JobDef { defName = "RimMind_Socialize" });
            DefDatabase<JobDef>.AddDef(new JobDef { defName = "RimMind_EmergencyTend" });
        }

        [Fact]
        public void SetPendingJob_ConsumePendingJob_RoundTrip()
        {
            var job = new Verse.AI.Job();
            _agent.SetPendingJob(job);

            var consumed = _agent.ConsumePendingJob();
            Assert.Same(job, consumed);

            var secondConsume = _agent.ConsumePendingJob();
            Assert.Null(secondConsume);
        }

        [Fact]
        public void RecordBehavior_AddsToHistory()
        {
            _agent.RecordBehavior(new BehaviorRecordDto
            {
                Action = "test_action",
                Reason = "testing",
                Success = true,
            });
            Assert.True(((IPawnAgent)_agent).BehaviorHistory.Count > 0);
        }

        [Fact]
        public void RecordBehavior_NullDto_DoesNothing()
        {
            _agent.RecordBehavior(null!);
            Assert.Equal(0, ((IPawnAgent)_agent).BehaviorHistory.Count);
        }

        private class TrackingActionBridge : IAgentActionBridge
    {
        private readonly Action _onExecute;

        public TrackingActionBridge(Action onExecute)
        {
            _onExecute = onExecute;
        }

        public bool CanExecute(string npcId, string actionName) => true;

        public bool CanExecute(object pawn, string action) => true;

        public void Execute(object pawn, string action, string? targetName = null)
        {
            _onExecute();
        }

        public void ExecuteAction(string npcId, string actionName, string[]? args = null)
        {
            _onExecute();
        }

        public List<StructuredTool>? GetAvailableTools(object pawn) => null;
    }
    }
}
