using System;
using RimMind.Domain.Events;
using RimMind.Presentation.Agent;
using RimMind.Infrastructure.Patches;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
using Verse;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class PawnActorTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly AgentGoalStack _goalStack;
        private readonly PawnRecorder _recorder;
        private readonly PawnActor _actor;
        private AgentState _state = AgentState.Active;

        public PawnActorTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 55, Dead = false };
            _pawn.jobs = new Pawn_JobTracker { jobQueue = new Verse.AI.JobQueue() };
            RimMindCoreMod.Settings = new AICoreSettings
            {
                Context = new ContextSettings(),
                maxTokens = 800,
                defaultTemperature = 0.7f,
                thinkCooldownTicks = 30000,
                agentTickInterval = 150,
                behaviorHistoryMax = 100,
            };
            _eventBus = new EventBusAdapter(new AgentBusImpl());
            _goalStack = new AgentGoalStack();
            _recorder = new PawnRecorder(_pawn, _eventBus, () => _state);
            _actor = new PawnActor(_pawn, _eventBus, _goalStack, _recorder);
        }

        public void Dispose()
        {
            _recorder.Cleanup();
            RimMindCoreMod.Settings = null;
        }

        [Fact]
        public void Execute_RecordsBehavior()
        {
            _actor.Execute("force_rest", null, "tired");
            Assert.True(_recorder.BehaviorHistory.Count > 0);
        }

        [Fact]
        public void Execute_RecordsActionAndReason()
        {
            _actor.Execute("force_rest", null, "need rest");
            var lastRecord = _recorder.BehaviorHistory[_recorder.BehaviorHistory.Count - 1];
            Assert.Equal("force_rest", lastRecord.Action);
            Assert.Equal("need rest", lastRecord.Reason);
        }

        [Fact]
        public void Execute_RecordsBehaviorWithAction()
        {
            _actor.Execute("force_rest", null, "tired");
            Assert.True(_recorder.BehaviorHistory.Count > 0);
            var lastRecord = _recorder.BehaviorHistory[_recorder.BehaviorHistory.Count - 1];
            Assert.Equal("force_rest", lastRecord.Action);
            Assert.Equal("tired", lastRecord.Reason);
        }

        [Fact]
        public void Execute_PublishesDecisionEvent()
        {
            _actor.Execute("force_rest", null, "tired");
            Assert.True(_recorder.BehaviorHistory.Count > 0);
        }

        [Fact]
        public void SetPendingJob_EnqueuesJob()
        {
            var job = new Verse.AI.Job();
            _actor.SetPendingJob(job);

            var queued = _pawn.jobs?.jobQueue?.FirstOrDefault(qj => qj.job?.jobGiver is ThinkNode_RimMindAgent);
            Assert.NotNull(queued);
        }

        [Fact]
        public void ComputeGoalProgressDelta_KnownAction_ReturnsPositiveForExecuted()
        {
            float delta = PawnActor.ComputeGoalProgressDelta("force_rest", true);
            Assert.True(delta > 0f);
        }

        [Fact]
        public void ComputeGoalProgressDelta_FailedAction_ReturnsNegative()
        {
            float delta = PawnActor.ComputeGoalProgressDelta("force_rest", false);
            Assert.True(delta < 0f);
        }

        [Fact]
        public void ComputeGoalProgressDelta_UnknownAction_ReturnsDefault()
        {
            float delta = PawnActor.ComputeGoalProgressDelta("unknown_action", true);
            Assert.Equal(0.1f, delta);
        }

        [Fact]
        public void RestoreOriginalDuty_DoesNotThrow_WhenNoPreviousDuty()
        {
            var exception = Record.Exception(() => _actor.RestoreOriginalDuty());
            Assert.Null(exception);
        }
    }
}
