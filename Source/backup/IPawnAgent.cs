using System.Collections.Generic;
using RimMind.Application.Common.Models.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Common;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using Verse;

namespace RimMind.Application.Common.Interfaces.Agent
{
    public interface IPawnAgent : IAgentControl, IExposable
    {
        Pawn Pawn { get; }
        AgentIdentity Identity { get; }
        AgentGoalStack GoalStack { get; }
        IReadOnlyList<BehaviorRecord> BehaviorHistory { get; }
        new IReadOnlyList<BehaviorRecord> GetRecentHistory(int count = 10);
        new float GetRecentSuccessRate(int count = 10);
        IStrategyOptimizer StrategyOptimizer { get; }
        IPerceptionBuffer PerceptionBuffer { get; }

        /// <summary>
        /// Current autonomy level for agent decisions.
        /// </summary>
        AgentAutonomyLevel AutonomyLevel { get; }

        /// <summary>
        /// Current workflow phase in the Perceive→Think→Act→Record cycle.
        /// </summary>
        AgentWorkflowPhase WorkflowPhase { get; }

        void AddGoal(AgentGoal goal);
        new Verse.AI.Job? ConsumePendingJob();
        void SetPendingJob(Verse.AI.Job job);

        /// <summary>
        /// Execute an AgentDecision by delegating to the internal PawnActor's IActionExecutor.
        /// </summary>
        Result<Unit, RimMindError> ExecuteDecision(AgentDecision decision);

        /// <summary>
        /// Transition the workflow phase. Used by collaborators (Thinker, Actor) to drive the cycle.
        /// </summary>
        void TransitionWorkflow(AgentWorkflowPhase target);
    }
}
