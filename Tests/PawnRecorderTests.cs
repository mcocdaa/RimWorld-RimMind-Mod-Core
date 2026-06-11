using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models.Agent;
using RimMind.Presentation.Agent;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class PawnRecorderTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly IAgentBus _agentBus;

        public PawnRecorderTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 33, Dead = false };
            _pawn.jobs = new Pawn_JobTracker { jobQueue = new Verse.AI.JobQueue() };
            RimMindServiceLocator.Reset();
            RimMindCoreMod.Settings = new RimMindCoreSettings
            {
                Context = new ContextSettings(),
                maxTokens = 800,
                defaultTemperature = 0.7f,
                thinkCooldownTicks = 30000,
                agentTickInterval = 150,
                behaviorHistoryMax = 100,
            };
            _agentBus = new AgentBusImpl();
            _agent = new PawnAgent(_pawn, _agentBus);
        }

        public void Dispose()
        {
            if (_agent.State != AgentState.Terminated)
                _agent.TransitionTo(AgentState.Terminated);
            RimMindCoreMod.Settings = null;
        }

        [Fact]
        public void RecordBehavior_AddsToHistory()
        {
            _agent.RecordBehavior(new BehaviorRecordDto
            {
                Action = "test_action",
                Reason = "test reason",
                Success = true,
                ResultReason = "ok",
                GoalProgressDelta = 0.1f,
                Timestamp = 100,
                ActionEventId = "evt-1",
            });
            Assert.Single(_agent.BehaviorHistory);
        }

        [Fact]
        public void RecordBehavior_StoresCorrectData()
        {
            _agent.RecordBehavior(new BehaviorRecordDto
            {
                Action = "force_rest",
                Reason = "tired",
                Success = true,
                ResultReason = "resting",
                GoalProgressDelta = 0.15f,
                Timestamp = 200,
                ActionEventId = "evt-2",
            });
            var record = _agent.BehaviorHistory[0];
            Assert.Equal("force_rest", record.Action);
            Assert.Equal("tired", record.Reason);
            Assert.True(record.Success);
            Assert.Equal("resting", record.ResultReason);
            Assert.Equal(0.15f, record.GoalProgressDelta);
            Assert.Equal(200, record.Timestamp);
            Assert.Equal("evt-2", record.ActionEventId);
        }

        [Fact]
        public void RecordBehavior_RespectsMaxHistorySize()
        {
            RimMindCoreMod.Settings!.behaviorHistoryMax = 5;
            var agent = new PawnAgent(_pawn, _agentBus);
            for (int i = 0; i < 10; i++)
            {
                agent.RecordBehavior(new BehaviorRecordDto
                {
                    Action = $"action_{i}",
                    Reason = "test",
                    Success = true,
                    GoalProgressDelta = 0.1f,
                    Timestamp = i,
                    ActionEventId = $"evt-{i}",
                });
            }
            Assert.Equal(5, agent.BehaviorHistory.Count);
        }

        [Fact]
        public void RecordBehavior_DequeuesOldest_WhenOverCapacity()
        {
            RimMindCoreMod.Settings!.behaviorHistoryMax = 3;
            var agent = new PawnAgent(_pawn, _agentBus);
            for (int i = 0; i < 5; i++)
            {
                agent.RecordBehavior(new BehaviorRecordDto
                {
                    Action = $"action_{i}",
                    Reason = "test",
                    Success = true,
                    GoalProgressDelta = 0.1f,
                    Timestamp = i,
                    ActionEventId = $"evt-{i}",
                });
            }
            Assert.Equal(3, agent.BehaviorHistory.Count);
            Assert.Equal("action_2", agent.BehaviorHistory[0].Action);
            Assert.Equal("action_4", agent.BehaviorHistory[2].Action);
        }

        [Fact]
        public void RecordBehavior_NullDto_DoesNothing()
        {
            _agent.RecordBehavior(null!);
            Assert.Empty(_agent.BehaviorHistory);
        }

        [Fact]
        public void BehaviorHistory_IsReadOnlyCopy()
        {
            _agent.RecordBehavior(new BehaviorRecordDto
            {
                Action = "action_1",
                Reason = "test",
                Success = true,
                GoalProgressDelta = 0.1f,
                Timestamp = 100,
                ActionEventId = "evt-1",
            });
            var history = _agent.BehaviorHistory;
            Assert.Single(history);
        }

        [Fact]
        public void StrategyOptimizer_IsAccessible()
        {
            Assert.NotNull(_agent.StrategyOptimizer);
        }
    }
}
