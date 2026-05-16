using System;
using RimMind.Domain.Events;
using RimMind.Presentation.Agent;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Extension;
using Verse;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class PawnRecorderTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly PawnRecorder _recorder;
        private AgentState _state = AgentState.Active;

        public PawnRecorderTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 33, Dead = false };
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
            _recorder = new PawnRecorder(_pawn, _eventBus, () => _state);
        }

        public void Dispose()
        {
            _recorder.Cleanup();
            RimMindCoreMod.Settings = null;
        }

        [Fact]
        public void Record_AddsToBehaviorHistory()
        {
            _recorder.Record("test_action", "test reason", true, "ok", 0.1f, 100, "evt-1");
            Assert.Single(_recorder.BehaviorHistory);
        }

        [Fact]
        public void Record_StoresCorrectData()
        {
            _recorder.Record("force_rest", "tired", true, "resting", 0.15f, 200, "evt-2");
            var record = _recorder.BehaviorHistory[0];
            Assert.Equal("force_rest", record.Action);
            Assert.Equal("tired", record.Reason);
            Assert.True(record.Success);
            Assert.Equal("resting", record.ResultReason);
            Assert.Equal(0.15f, record.GoalProgressDelta);
            Assert.Equal(200, record.Timestamp);
            Assert.Equal("evt-2", record.ActionEventId);
        }

        [Fact]
        public void Record_RespectsMaxHistorySize()
        {
            RimMindCoreMod.Settings!.behaviorHistoryMax = 5;
            for (int i = 0; i < 10; i++)
                _recorder.Record($"action_{i}", "test", true, "ok", 0.1f, i, $"evt-{i}");

            Assert.Equal(5, _recorder.BehaviorHistory.Count);
        }

        [Fact]
        public void Record_DequeuesOldest_WhenOverCapacity()
        {
            RimMindCoreMod.Settings!.behaviorHistoryMax = 3;
            for (int i = 0; i < 5; i++)
                _recorder.Record($"action_{i}", "test", true, "ok", 0.1f, i, $"evt-{i}");

            Assert.Equal(3, _recorder.BehaviorHistory.Count);
            Assert.Equal("action_2", _recorder.BehaviorHistory[0].Action);
            Assert.Equal("action_4", _recorder.BehaviorHistory[2].Action);
        }

        [Fact]
        public void BehaviorHistory_IsReadOnlyCopy()
        {
            _recorder.Record("action_1", "test", true, "ok", 0.1f, 100, "evt-1");
            var history = _recorder.BehaviorHistory;
            Assert.Single(history);
        }

        [Fact]
        public void StrategyOptimizer_IsAccessible()
        {
            Assert.NotNull(_recorder.StrategyOptimizer);
        }

        [Fact]
        public void AdjustStrategyWeight_ModifiesOptimizer()
        {
            _recorder.AdjustStrategyWeight("force_rest", 0.5f);
            var top = _recorder.StrategyOptimizer.GetTopN(1);
            Assert.Single(top);
            Assert.Equal("force_rest", top[0].Key);
        }

        [Fact]
        public void Cleanup_UnsubscribesFromEventBus()
        {
            _recorder.Cleanup();

            var evt = new ActionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "test_action", true, "ok", "evt-cleanup");
            _eventBus.Publish(evt);

            var historyCountBefore = _recorder.BehaviorHistory.Count;
            _recorder.Record("after_cleanup", "test", true, "ok", 0.1f, 300, "evt-after");
            Assert.Equal(historyCountBefore + 1, _recorder.BehaviorHistory.Count);
        }

        [Fact]
        public void Resubscribe_ReceivesActionEvents()
        {
            _recorder.Cleanup();
            _recorder.Resubscribe();

            var evt = new ActionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "force_rest", true, "ok", "evt-resub");
            _eventBus.Publish(evt);

            var top = _recorder.StrategyOptimizer.GetTopN(1);
            Assert.Single(top);
        }

        [Fact]
        public void OnActionEvent_IgnoresWhenNotActive()
        {
            _state = AgentState.Dormant;

            var evt = new ActionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "force_rest", true, "ok", "evt-dormant");
            _eventBus.Publish(evt);

            var top = _recorder.StrategyOptimizer.GetTopN(1);
            Assert.Empty(top);
        }

        [Fact]
        public void OnActionEvent_IgnoresEventsForOtherPawns()
        {
            var evt = new ActionEvent("NPC-999", 999, "force_rest", true, "ok", "evt-other");
            _eventBus.Publish(evt);

            var top = _recorder.StrategyOptimizer.GetTopN(1);
            Assert.Empty(top);
        }
    }
}
