using System;
using System.Collections.Generic;
using RimMind.Core;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using RimMind.Core.Client;
using RimMind.Core.Extensions;
using RimMind.Kernel.Flywheel;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using RimMind.Core.Settings;
using Verse;
using Verse.AI;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PublishDecisionJobEnqueueTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly IFlywheelParameterStore? _originalFlywheel;
        private readonly IAgentActionBridge? _originalBridge;
        private readonly NpcManager _npcManager;

        public PublishDecisionJobEnqueueTests()
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
            _agent = new PawnAgent(_pawn, new EventBusAdapter(new AgentBusImpl()));
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
        public void PublishDecisionAndRecord_EnqueuesJobInPawnJobQueue()
        {
            _agent.PublishDecisionAndRecord("force_rest", null, "tired");

            Assert.NotNull(_pawn.jobs?.jobQueue);
            Assert.True(_pawn.jobs.jobQueue.Count > 0, "Job should be enqueued in pawn's job queue");
        }

        [Fact]
        public void PublishDecisionAndRecord_CreatesJobViaJobMaker()
        {
            _agent.PublishDecisionAndRecord("force_rest", null, "tired");

            var queuedJob = _pawn.jobs?.jobQueue?.Peek();
            Assert.NotNull(queuedJob);
            Assert.NotNull(queuedJob.job);
            Assert.True(queuedJob.job.createdViaJobMaker,
                "Job should be created via JobMaker.MakeJob(), not new Job()");
        }

        [Fact]
        public void PublishDecisionAndRecord_SetsJobDefFromAction()
        {
            _agent.PublishDecisionAndRecord("force_rest", null, "tired");

            var queuedJob = _pawn.jobs?.jobQueue?.Peek();
            Assert.NotNull(queuedJob?.job?.def);
            Assert.Equal("RimMind_Rest", queuedJob.job.def.defName);
        }

        [Fact]
        public void PublishDecisionAndRecord_FallbackToGenericAction()
        {
            _agent.PublishDecisionAndRecord("unknown_action", null, "testing");

            var queuedJob = _pawn.jobs?.jobQueue?.Peek();
            Assert.NotNull(queuedJob?.job?.def);
            Assert.Equal("RimMind_GenericAction", queuedJob.job.def.defName);
        }

        [Fact]
        public void PublishDecisionAndRecord_DoesNotCallBridgeExecuteDirectly()
        {
            bool bridgeCalled = false;
            var trackingBridge = new TrackingActionBridge(() => bridgeCalled = true);
            RimMindAPI.RegisterAgentActionBridge(trackingBridge);

            _agent.PublishDecisionAndRecord("force_rest", null, "tired");

            Assert.False(bridgeCalled,
                "PublishDecisionAndRecord should NOT call bridge.Execute() directly; execution should happen in JobDriver");
        }

        [Fact]
        public void PublishDecisionAndRecord_SetsJobGiverForConsumePendingJob()
        {
            _agent.PublishDecisionAndRecord("force_rest", null, "tired");

            var queuedJob = _pawn.jobs?.jobQueue?.Peek();
            Assert.NotNull(queuedJob?.job?.jobGiver);
            Assert.IsType<ThinkNode_RimMindAgent>(queuedJob.job.jobGiver);
        }

        [Fact]
        public void PublishDecisionAndRecord_WithTarget_SetsTargetA()
        {
            var targetPawn = new Pawn { thingIDNumber = 99 };
            targetPawn.jobs = new Pawn_JobTracker { jobQueue = new JobQueue() };
            _npcManager.IndexPawn(targetPawn);
            var map = new Map { mapPawns = new MapPawns() };
            map.mapPawns.AllPawns.Add(targetPawn);
            _pawn.Map = map;

            _agent.PublishDecisionAndRecord("tend_pawn", "99", "injured");

            var queuedJob = _pawn.jobs?.jobQueue?.Peek();
            Assert.NotNull(queuedJob?.job);
            Assert.Equal(targetPawn, queuedJob.job.targetA.Thing as Pawn);
        }

        private class TrackingActionBridge : IAgentActionBridge
    {
        private readonly Action _onExecute;

        public TrackingActionBridge(Action onExecute)
        {
            _onExecute = onExecute;
        }

        public bool CanExecute(Pawn pawn, string action) => true;

        public void Execute(Pawn pawn, string action, string? targetName = null)
        {
            _onExecute();
        }

        public List<StructuredTool>? GetAvailableTools(Pawn pawn) => null;
    }
    }
}
