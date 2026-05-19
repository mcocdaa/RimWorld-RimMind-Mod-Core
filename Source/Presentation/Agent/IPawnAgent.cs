using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using Verse;

namespace RimMind.Presentation.Agent
{
    public interface IPawnAgent : IAgentControl, IExposable
    {
        Pawn Pawn { get; }
        AgentIdentity Identity { get; }
        AgentGoalStack GoalStack { get; }
        IReadOnlyList<BehaviorRecord> BehaviorHistory { get; }
        StrategyOptimizer StrategyOptimizer { get; }
        PerceptionBuffer PerceptionBuffer { get; }
        void AddGoal(AgentGoal goal);
        Verse.AI.Job? ConsumePendingJob();
        void SetPendingJob(Verse.AI.Job job);
    }
}
