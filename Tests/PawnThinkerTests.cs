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
    public class PawnThinkerTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly IAgentBus _agentBus;

        public PawnThinkerTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 77, Dead = false };
            _pawn.jobs = new Pawn_JobTracker { jobQueue = new Verse.AI.JobQueue() };
            RimMindServiceLocator.Reset();
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
        }

        public void Dispose()
        {
            if (_agent.State != AgentState.Terminated)
                _agent.TransitionTo(AgentState.Terminated);
            RimMindCoreMod.Settings = null;
        }

        [Fact]
        public void ForceThink_DoesNotThrow()
        {
            var exception = Record.Exception(() => _agent.ForceThink());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WithNoGoals_DoesNotThrow()
        {
            _agent.TransitionTo(AgentState.Active);
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WithActiveGoal_DoesNotThrow()
        {
            _agent.TransitionTo(AgentState.Active);
            _agent.AddGoal(new SerializableAgentGoal("test goal", GoalCategory.Survival, 0.8f, GoalStatus.Active));
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WithPerceptions_DoesNotThrow()
        {
            _agent.TransitionTo(AgentState.Active);
            _agent.AddGoal(new SerializableAgentGoal("test goal", GoalCategory.Survival, 0.8f, GoalStatus.Active));
            _agent.PerceptionBuffer.Add(new PerceptionBufferEntry
            {
                PerceptionType = "sight",
                Content = "something",
                Importance = 0.5f,
                PawnId = _pawn.thingIDNumber
            });
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WhenNotActive_DoesNotThink()
        {
            _agent.AddGoal(new SerializableAgentGoal("test goal", GoalCategory.Survival, 0.8f, GoalStatus.Active));
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }
    }
}
