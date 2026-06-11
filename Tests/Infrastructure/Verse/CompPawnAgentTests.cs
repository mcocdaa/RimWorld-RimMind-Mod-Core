using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
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
    public class CompPawnAgentGizmoTests
    {
        private static CompPawnAgent CreateCompWithAgent(IPawnAgentVerse? agent)
        {
            return new CompPawnAgent { Agent = agent };
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
            public Pawn Pawn => throw new NotImplementedException();
            public AgentIdentity Identity => throw new NotImplementedException();
            public IReadOnlyList<BehaviorRecord> BehaviorHistory => Array.Empty<BehaviorRecord>();
            public AgentGoalStack GoalStack => throw new NotImplementedException();
            public IStrategyOptimizer StrategyOptimizer => throw new NotImplementedException();
            public IPerceptionBuffer PerceptionBuffer => throw new NotImplementedException();
            public AgentAutonomyLevel AutonomyLevel { get => AgentAutonomyLevel.Autonomous; set { } }
            public AgentWorkflowPhase WorkflowPhase => AgentWorkflowPhase.Idle;

            public void Tick() { }
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
