using System;
using RimMind.Contracts;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using RimMind.Core.Runtime;
using RimMind.Contracts.Settings;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PawnThinkerTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly AgentGoalStack _goalStack;
        private readonly PawnRecorder _recorder;
        private readonly PawnActor _actor;
        private readonly PawnThinker _thinker;
        private AgentState _state = AgentState.Active;

        public PawnThinkerTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 77, Dead = false };
            _pawn.jobs = new Pawn_JobTracker { jobQueue = new Verse.AI.JobQueue() };
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
            _eventBus = new EventBusAdapter(new AgentBusImpl());
            _goalStack = new AgentGoalStack();
            _recorder = new PawnRecorder(_pawn, _eventBus, () => _state);
            _actor = new PawnActor(_pawn, _eventBus, _goalStack, _recorder);
            _thinker = new PawnThinker(_pawn, _eventBus, _goalStack, _actor, _recorder);
        }

        public void Dispose()
        {
            _recorder.Cleanup();
            RimMindCoreMod.Settings = null;
        }

        [Fact]
        public void Constructor_InitializesLastThinkTick()
        {
            Assert.True(_thinker.LastThinkTick <= 0);
        }

        [Fact]
        public void ForceThink_ResetsLastThinkTick()
        {
            _thinker.ForceThink();
            Assert.True(_thinker.LastThinkTick <= 0);
        }

        [Fact]
        public void Think_WithNoGoals_DoesNotThrow()
        {
            var emptyPerceptions = Array.Empty<PerceptionBufferEntry>();
            var exception = Record.Exception(() => _thinker.Think(emptyPerceptions));
            Assert.Null(exception);
        }

        [Fact]
        public void Think_WithActiveGoal_DoesNotThrow()
        {
            _goalStack.TryAdd(new AgentGoal("test goal", GoalCategory.Survival, 0.8f, GoalStatus.Active), _pawn.thingIDNumber);
            var emptyPerceptions = Array.Empty<PerceptionBufferEntry>();
            var exception = Record.Exception(() => _thinker.Think(emptyPerceptions));
            Assert.Null(exception);
        }

        [Fact]
        public void Think_WithPerceptions_DoesNotThrow()
        {
            _goalStack.TryAdd(new AgentGoal("test goal", GoalCategory.Survival, 0.8f, GoalStatus.Active), _pawn.thingIDNumber);
            var perceptions = new[]
            {
                new PerceptionBufferEntry { PerceptionType = "sight", Content = "something", Importance = 0.5f, PawnId = _pawn.thingIDNumber }
            };
            var exception = Record.Exception(() => _thinker.Think(perceptions));
            Assert.Null(exception);
        }
    }
}
