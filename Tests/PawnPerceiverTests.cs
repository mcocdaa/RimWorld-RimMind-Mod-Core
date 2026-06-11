using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Presentation.Agent;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class PawnPerceiverTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly IAgentBus _agentBus;

        public PawnPerceiverTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 42, Dead = false };
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
        public void PerceptionBuffer_IsAccessible()
        {
            Assert.NotNull(_agent.PerceptionBuffer);
        }

        [Fact]
        public void PerceptionBuffer_InitiallyEmpty()
        {
            Assert.Equal(0, _agent.PerceptionBuffer.Count);
        }

        [Fact]
        public void Tick_WhenNotActive_DoesNotThrow()
        {
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void Tick_WhenActive_DoesNotThrow()
        {
            _agent.TransitionTo(AgentState.Active);
            var exception = Record.Exception(() => _agent.Tick());
            Assert.Null(exception);
        }

        [Fact]
        public void PerceptionBuffer_AddAndFlush()
        {
            _agent.PerceptionBuffer.Add(new PerceptionBufferEntry
            {
                PerceptionType = "sight",
                Content = "saw something",
                Importance = 0.5f,
                PawnId = _pawn.thingIDNumber
            });
            Assert.Equal(1, _agent.PerceptionBuffer.Count);

            var flushed = _agent.PerceptionBuffer.Flush();
            Assert.Single(flushed);
            Assert.Equal(0, _agent.PerceptionBuffer.Count);
        }

        [Fact]
        public void PerceptionBuffer_Clear()
        {
            _agent.PerceptionBuffer.Add(new PerceptionBufferEntry
            {
                PerceptionType = "sight",
                Content = "saw something",
                Importance = 0.5f,
                PawnId = _pawn.thingIDNumber
            });
            _agent.PerceptionBuffer.Clear();
            Assert.Equal(0, _agent.PerceptionBuffer.Count);
        }
    }
}
