using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Features.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Common;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation.Agent;
using Verse;
using Verse.AI;
using Xunit;

namespace RimMind.Tests.Infrastructure.Verse
{
    public class CompPawnAgentGizmoTests : IDisposable
    {
        public CompPawnAgentGizmoTests()
        {
            RimMindServiceLocator.Reset();
        }

        public void Dispose()
        {
            RimMindServiceLocator.Reset();
        }

        private static CompPawnAgent CreateCompWithAgent(IPawnAgentVerse? agent)
        {
            return new CompPawnAgent { Agent = agent };
        }

        private static CompPawnAgent CreateAttachedComp(IPawnAgentVerse? agent)
        {
            var pawn = new Pawn { thingIDNumber = 42 };
            var comp = new CompPawnAgent();
            pawn.AddComp(comp);
            comp.Agent = agent;
            return comp;
        }

        private static CompPawnAgent CreateCompAwaitingTickRegistration(IPawnAgentVerse agent)
        {
            var comp = new CompPawnAgent { Agent = agent };
            var pawn = new Pawn { thingIDNumber = 42 };
            pawn.AddComp(comp);
            return comp;
        }

        private sealed class StubAgentControl : IPawnAgentVerse
        {
            private AgentState _state;

            public StubAgentControl(AgentState state)
            {
                _state = state;
            }

            public bool IsActive => _state == AgentState.Active;
            public AgentState State => _state;
            public AgentModeId CurrentModeId => AgentModeId.Reactive;
            public IAgentMode CurrentMode => throw new NotImplementedException();
            public bool IsPawnValid => true;
            public string GetDebugInfo() => "stub";
            public string NpcId => "TEST-1";
            public string Label => "TestPawn";
            public int? LastThinkTick { get; set; }
            public int GoalCount => 0;
            public int TickCount { get; private set; }
            public Pawn Pawn => throw new NotImplementedException();
            public AgentIdentity Identity => throw new NotImplementedException();
            public IReadOnlyList<BehaviorRecord> BehaviorHistory => Array.Empty<BehaviorRecord>();
            public AgentGoalStack GoalStack => throw new NotImplementedException();
            public IStrategyOptimizer StrategyOptimizer => throw new NotImplementedException();
            public IPerceptionBuffer PerceptionBuffer => throw new NotImplementedException();
            public AgentAutonomyLevel AutonomyLevel { get => AgentAutonomyLevel.Autonomous; set { } }
            public AgentWorkflowPhase WorkflowPhase => AgentWorkflowPhase.Idle;

            public void Tick() { TickCount++; }
            public bool TransitionTo(AgentState newState) { _state = newState; return true; }
            public void ForceThink() { }
            public void SwitchMode(AgentModeId modeId) { }
            public void Cleanup() { }
            public void Destroy() { }
            public void ResubscribeEvents() { }
            public bool RemoveGoal(string goalDescription) => false;
            public void AddGoal(AgentGoal goal) { }
            public void RecordBehavior(BehaviorRecordDto record) { }
            public global::Verse.AI.Job? ConsumePendingJob() => null;
            public void SetPendingJob(global::Verse.AI.Job job) { }
            object? IJobProvider.ConsumePendingJob() => null;
            public void ExposeData() { }
            public Result<Unit, RimMindError> ExecuteDecision(AgentDecision decision) => throw new NotImplementedException();
            public void TransitionWorkflow(AgentWorkflowPhase target) { }
            IReadOnlyList<BehaviorRecord> IPawnAgent.GetRecentHistory(int count) => Array.Empty<BehaviorRecord>();
            float IPawnAgent.GetRecentSuccessRate(int count) => 1.0f;
            public IReadOnlyList<BehaviorRecordDto> GetRecentHistory(int count = 10) => Array.Empty<BehaviorRecordDto>();
            public float GetRecentSuccessRate(int count = 10) => 1.0f;
        }

        [Fact]
        public void CompTick_RegistersAgentWithoutTickingItDirectly()
        {
            var scheduler = new AgentLoopScheduler();
            RimMindServiceLocator.Register<IAgentLoopScheduler>(scheduler);
            var agent = new StubAgentControl(AgentState.Active);
            var comp = CreateCompAwaitingTickRegistration(agent);

            Assert.Null(scheduler.Find(AgentLoopKeys.ForPawn(42)));

            comp.CompTick();

            Assert.Same(agent, scheduler.Find(AgentLoopKeys.ForPawn(42)));
            Assert.Equal(0, agent.TickCount);
        }

        [Fact]
        public void CompTick_RepeatedCallsKeepOneRegistrationAndSchedulerTicksOncePerGameTick()
        {
            var scheduler = new AgentLoopScheduler();
            RimMindServiceLocator.Register<IAgentLoopScheduler>(scheduler);
            var agent = new StubAgentControl(AgentState.Active);
            var comp = CreateCompAwaitingTickRegistration(agent);

            comp.CompTick();
            comp.CompTick();
            scheduler.Tick(100);
            scheduler.Tick(100);

            Assert.Same(agent, scheduler.Find(AgentLoopKeys.ForPawn(42)));
            Assert.Equal(1, scheduler.GetSnapshot().RegisteredPawnAgents);
            Assert.Equal(1, agent.TickCount);
        }

        [Fact]
        public void Agent_AssignedNull_UnregistersPreviousLoopEntry()
        {
            var scheduler = new AgentLoopScheduler();
            RimMindServiceLocator.Register<IAgentLoopScheduler>(scheduler);
            var comp = CreateAttachedComp(new StubAgentControl(AgentState.Active));
            var key = AgentLoopKeys.ForPawn(42);
            Assert.NotNull(scheduler.Find(key));

            comp.Agent = null;

            Assert.Null(scheduler.Find(key));
        }

        [Fact]
        public void CompTick_TerminatedAgent_UnregistersWithoutReregistering()
        {
            var scheduler = new AgentLoopScheduler();
            RimMindServiceLocator.Register<IAgentLoopScheduler>(scheduler);
            var agent = new StubAgentControl(AgentState.Active);
            var comp = CreateAttachedComp(agent);
            var key = AgentLoopKeys.ForPawn(42);
            Assert.Same(agent, scheduler.Find(key));

            agent.TransitionTo(AgentState.Terminated);
            comp.CompTick();

            Assert.Null(scheduler.Find(key));
            Assert.Equal(0, scheduler.GetSnapshot().RegisteredPawnAgents);
        }

        [Fact]
        public void Agent_Replaced_UpdatesSchedulerEntryToReplacement()
        {
            var scheduler = new AgentLoopScheduler();
            RimMindServiceLocator.Register<IAgentLoopScheduler>(scheduler);
            var original = new StubAgentControl(AgentState.Active);
            var replacement = new StubAgentControl(AgentState.Active);
            var comp = CreateAttachedComp(original);
            var key = AgentLoopKeys.ForPawn(42);
            Assert.Same(original, scheduler.Find(key));

            comp.Agent = replacement;

            Assert.Same(replacement, scheduler.Find(key));
            scheduler.Tick(200);
            Assert.Equal(0, original.TickCount);
            Assert.Equal(1, replacement.TickCount);
        }

        [Fact]
        public void CompTick_RetriesRegistrationAfterSchedulerBecomesAvailable()
        {
            var agent = new StubAgentControl(AgentState.Active);
            var comp = CreateAttachedComp(agent);
            var scheduler = new AgentLoopScheduler();
            Assert.False(RimMindServiceLocator.IsRegistered<IAgentLoopScheduler>());

            RimMindServiceLocator.Register<IAgentLoopScheduler>(scheduler);
            comp.CompTick();

            Assert.Same(agent, scheduler.Find(AgentLoopKeys.ForPawn(42)));
            Assert.Equal(0, agent.TickCount);
        }

        [Fact]
        public void CompGetGizmosExtra_NullAgent_YieldsOneAgentControlGizmo()
        {
            var comp = new CompPawnAgent { Agent = null };
            var gizmos = comp.CompGetGizmosExtra().OfType<Command_Action>().ToList();
            Assert.Single(gizmos);
            Assert.Contains(gizmos, g => g.defaultLabel.Contains("Control"));
        }

        [Fact]
        public void CompGetGizmosExtra_ActiveAgent_YieldsOneAgentControlGizmo()
        {
            var agent = new StubAgentControl(AgentState.Active);
            var comp = CreateCompWithAgent(agent);

            var gizmos = comp.CompGetGizmosExtra().OfType<Command_Action>().ToList();

            Assert.Single(gizmos);
            Assert.Contains(gizmos, g => g.defaultLabel.Contains("Control"));
        }

        [Fact]
        public void CompGetGizmosExtra_DormantAgent_YieldsOneAgentControlGizmo()
        {
            var agent = new StubAgentControl(AgentState.Dormant);
            var comp = CreateCompWithAgent(agent);

            var gizmos = comp.CompGetGizmosExtra().OfType<Command_Action>().ToList();

            Assert.Single(gizmos);
            Assert.Contains(gizmos, g => g.defaultLabel.Contains("Control"));
            Assert.DoesNotContain(gizmos, g => g.defaultLabel.Contains("Dialogue"));
            Assert.DoesNotContain(gizmos, g => g.defaultLabel.Contains("Mode"));
        }

        [Fact]
        public void CompGetGizmosExtra_AgentControl_DoesNotCreateAgent()
        {
            var comp = new CompPawnAgent { Agent = null };
            var gizmos = comp.CompGetGizmosExtra().OfType<Command_Action>().ToList();
            var controlGizmo = gizmos.First(g => g.defaultLabel.Contains("Control"));

            // Clicking Agent Control should not create an agent
            Assert.Null(comp.Agent);
            Assert.NotNull(controlGizmo.action);

            // Actually invoke the action — it opens a window but must not create an Agent
            controlGizmo.action!();
            Assert.Null(comp.Agent);
        }
    }
}
