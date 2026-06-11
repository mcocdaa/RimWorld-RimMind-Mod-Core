using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models.Agent;
using RimMind.Presentation.Agent;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;
using Verse.AI;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class PawnActorTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly PawnAgent _agent;
        private readonly IAgentBus _agentBus;

        public PawnActorTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 55, Dead = false };
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
        public void RecordBehavior_RecordsActionAndReason()
        {
            _agent.RecordBehavior(new BehaviorRecordDto
            {
                Action = "force_rest",
                Reason = "need rest",
                Success = true,
                Timestamp = 100,
            });
            Assert.True(_agent.BehaviorHistory.Count > 0);
            var lastRecord = _agent.BehaviorHistory[_agent.BehaviorHistory.Count - 1];
            Assert.Equal("force_rest", lastRecord.Action);
            Assert.Equal("need rest", lastRecord.Reason);
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
    }
}
