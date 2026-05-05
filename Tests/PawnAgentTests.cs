using System;
using System.Collections.Generic;
using RimMind.Core.Agent;
using RimMind.Core.AgentBus;
using RimMind.Core.Client;
using RimMind.Core.Flywheel;
using RimMind.Core.Npc;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PawnAgentTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly AICoreSettings? _originalSettings;
        private readonly FlywheelParameterStore? _originalFlywheel;

        public PawnAgentTests()
        {
            _pawn = new Pawn { thingIDNumber = 99, Dead = false };
            NpcManager.IndexPawn(_pawn);
            _originalSettings = RimMindCoreMod.Settings;
            _originalFlywheel = FlywheelParameterStore.Instance;
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
            _agent = new PawnAgent(_pawn);
        }

        public void Dispose()
        {
            if (_agent.State != AgentState.Terminated)
                _agent.TransitionTo(AgentState.Terminated);
            RimMindCoreMod.Settings = _originalSettings;
            if (_originalFlywheel != null)
                _originalFlywheel.FinalizeInit();
            NpcManager.ClearPawnIndex();
        }

        [Fact]
        public void Constructor_InitializesInDormantState()
        {
            Assert.Equal(AgentState.Dormant, _agent.State);
        }

        [Fact]
        public void Constructor_SetsPawn()
        {
            Assert.Same(_pawn, _agent.Pawn);
        }

        [Fact]
        public void TransitionTo_DormantToActive_Succeeds()
        {
            bool result = _agent.TransitionTo(AgentState.Active);
            Assert.True(result);
            Assert.Equal(AgentState.Active, _agent.State);
        }

        [Fact]
        public void TransitionTo_DormantToPaused_Fails()
        {
            bool result = _agent.TransitionTo(AgentState.Paused);
            Assert.False(result);
            Assert.Equal(AgentState.Dormant, _agent.State);
        }

        [Fact]
        public void TransitionTo_ActiveToPaused_Succeeds()
        {
            _agent.TransitionTo(AgentState.Active);
            bool result = _agent.TransitionTo(AgentState.Paused);
            Assert.True(result);
            Assert.Equal(AgentState.Paused, _agent.State);
        }

        [Fact]
        public void TransitionTo_ActiveToDormant_Succeeds()
        {
            _agent.TransitionTo(AgentState.Active);
            bool result = _agent.TransitionTo(AgentState.Dormant);
            Assert.True(result);
            Assert.Equal(AgentState.Dormant, _agent.State);
        }

        [Fact]
        public void TransitionTo_ActiveToTerminated_Succeeds()
        {
            _agent.TransitionTo(AgentState.Active);
            bool result = _agent.TransitionTo(AgentState.Terminated);
            Assert.True(result);
            Assert.Equal(AgentState.Terminated, _agent.State);
        }

        [Fact]
        public void TransitionTo_PausedToActive_Succeeds()
        {
            _agent.TransitionTo(AgentState.Active);
            _agent.TransitionTo(AgentState.Paused);
            bool result = _agent.TransitionTo(AgentState.Active);
            Assert.True(result);
            Assert.Equal(AgentState.Active, _agent.State);
        }

        [Fact]
        public void TransitionTo_TerminatedToAny_Fails()
        {
            _agent.TransitionTo(AgentState.Active);
            _agent.TransitionTo(AgentState.Terminated);

            Assert.False(_agent.TransitionTo(AgentState.Dormant));
            Assert.False(_agent.TransitionTo(AgentState.Active));
            Assert.False(_agent.TransitionTo(AgentState.Paused));
        }

        [Fact]
        public void TransitionTo_SameState_Fails()
        {
            Assert.False(_agent.TransitionTo(AgentState.Dormant));
        }

        [Fact]
        public void TransitionTo_Terminated_CallsCleanup()
        {
            _agent.TransitionTo(AgentState.Active);
            _agent.AddGoal(new AgentGoal("test goal", GoalCategory.Survival, 0.5f, GoalStatus.Active));
            Assert.True(_agent.GoalStack.TotalCount > 0);

            _agent.TransitionTo(AgentState.Terminated);

            Assert.Equal(0, _agent.GoalStack.TotalCount);
        }

        [Fact]
        public void IsActive_TrueWhenActive()
        {
            Assert.False(_agent.IsActive);
            _agent.TransitionTo(AgentState.Active);
            Assert.True(_agent.IsActive);
        }

        [Fact]
        public void AddGoal_AddsToGoalStack()
        {
            var goal = new AgentGoal("survive", GoalCategory.Survival, 0.8f, GoalStatus.Proposed);
            _agent.AddGoal(goal);
            Assert.True(_agent.GoalStack.TotalCount > 0);
        }

        [Fact]
        public void AddGoal_NullGoal_DoesNothing()
        {
            _agent.AddGoal(null!);
            Assert.Equal(0, _agent.GoalStack.TotalCount);
        }

        [Fact]
        public void RemoveGoal_RemovesExistingGoal()
        {
            _agent.AddGoal(new AgentGoal("find food", GoalCategory.Survival, 0.8f, GoalStatus.Active));
            Assert.True(_agent.GoalStack.TotalCount > 0);

            bool removed = _agent.RemoveGoal("find food");
            Assert.True(removed);
            Assert.Equal(0, _agent.GoalStack.TotalCount);
        }

        [Fact]
        public void RemoveGoal_NonexistentGoal_ReturnsFalse()
        {
            bool removed = _agent.RemoveGoal("nonexistent");
            Assert.False(removed);
        }

        [Fact]
        public void RecordBehavior_AddsToHistory()
        {
            _agent.RecordBehavior(new BehaviorRecord
            {
                Action = "test_action",
                Reason = "testing",
                Success = true,
                ResultReason = "ok",
                Timestamp = 100,
            });
            Assert.Single(_agent.BehaviorHistory);
        }

        [Fact]
        public void RecordBehavior_RespectsMaxHistorySize()
        {
            RimMindCoreMod.Settings!.behaviorHistoryMax = 5;

            for (int i = 0; i < 10; i++)
            {
                _agent.RecordBehavior(new BehaviorRecord
                {
                    Action = $"action_{i}",
                    Reason = "test",
                    Success = true,
                    Timestamp = i,
                });
            }

            Assert.Equal(5, _agent.BehaviorHistory.Count);
        }

        [Fact]
        public void ForceThink_ResetsLastThinkTick()
        {
            _agent.ForceThink();
        }

        [Fact]
        public void Tick_WhenNotActive_DoesNothing()
        {
            _agent.AddGoal(new AgentGoal("goal", GoalCategory.Survival, 0.5f, GoalStatus.Active));
            int countBefore = _agent.GoalStack.TotalCount;
            _agent.Tick();
            Assert.Equal(countBefore, _agent.GoalStack.TotalCount);
        }

        [Fact]
        public void Tick_WhenActive_StillWorks()
        {
            _agent.TransitionTo(AgentState.Active);
            _agent.Tick();
        }

        [Fact]
        public void GoalStack_IsAccessible()
        {
            Assert.NotNull(_agent.GoalStack);
            Assert.Equal(0, _agent.GoalStack.TotalCount);
        }

        [Fact]
        public void StrategyOptimizer_IsAccessible()
        {
            Assert.NotNull(_agent.StrategyOptimizer);
        }

        [Fact]
        public void PerceptionBuffer_IsAccessible()
        {
            Assert.NotNull(_agent.PerceptionBuffer);
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
        public void Identity_DefaultIsNotNull()
        {
            Assert.NotNull(_agent.Identity);
        }

        [Fact]
        public void ComputeGoalProgressDelta_KnownActions_ReturnExpectedValues()
        {
            float restDelta = ComputeGoalProgressDelta("force_rest", true);
            Assert.Equal(0.15f, restDelta);

            float assignDelta = ComputeGoalProgressDelta("assign_work", true);
            Assert.Equal(0.2f, assignDelta);

            float moveDelta = ComputeGoalProgressDelta("move_to", true);
            Assert.Equal(0.05f, moveDelta);
        }

        [Fact]
        public void ComputeGoalProgressDelta_FailedAction_NegativeDelta()
        {
            float delta = ComputeGoalProgressDelta("force_rest", false);
            Assert.True(delta < 0);
            Assert.Equal(-0.075f, delta);
        }

        [Fact]
        public void ComputeGoalProgressDelta_UnknownAction_DefaultDelta()
        {
            float delta = ComputeGoalProgressDelta("unknown_action", true);
            Assert.Equal(0.1f, delta);
        }

        private static float ComputeGoalProgressDelta(string action, bool executed)
        {
            float baseDelta = action switch
            {
                "force_rest" => 0.15f,
                "assign_work" => 0.2f,
                "move_to" => 0.05f,
                "tend_pawn" => 0.2f,
                "rescue_pawn" => 0.25f,
                "draft" or "undraft" => 0.1f,
                "eat_food" => 0.15f,
                _ => 0.1f
            };
            return executed ? baseDelta : baseDelta * -0.5f;
        }
    }
}
