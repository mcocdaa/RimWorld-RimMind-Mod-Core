using System.Collections.Generic;
using Verse;

namespace RimMind.Presentation.Agent
{
    public interface IPawnAgent : IExposable
    {
        Pawn Pawn { get; }
        AgentState State { get; }
        AgentIdentity Identity { get; }
        AgentGoalStack GoalStack { get; }
        IReadOnlyList<BehaviorRecord> BehaviorHistory { get; }
        StrategyOptimizer StrategyOptimizer { get; }
        PerceptionBuffer PerceptionBuffer { get; }
        bool IsActive { get; }

        void Tick();
        bool TransitionTo(AgentState newState);
        void AddGoal(AgentGoal goal);
        void ForceThink();
        Verse.AI.Job? ConsumePendingJob();
        void SetPendingJob(Verse.AI.Job job);
        bool RemoveGoal(string goalDescription);
        void RecordBehavior(BehaviorRecord record);
        void Cleanup();
        void ResubscribeEvents();
    }
}
