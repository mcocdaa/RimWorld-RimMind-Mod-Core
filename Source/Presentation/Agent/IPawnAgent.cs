using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using Verse;

namespace RimMind.Presentation.Agent
{
    public interface IPawnAgent : IAgentInfo, IExposable
    {
        Pawn Pawn { get; }
        AgentIdentity Identity { get; }
        AgentGoalStack GoalStack { get; }
        IReadOnlyList<BehaviorRecord> BehaviorHistory { get; }
        StrategyOptimizer StrategyOptimizer { get; }
        PerceptionBuffer PerceptionBuffer { get; }
        bool IsActive { get; }

        AgentModeId CurrentModeId { get; }
        IAgentMode CurrentMode { get; }

        void Tick();
        bool TransitionTo(AgentState newState);
        void AddGoal(AgentGoal goal);
        void ForceThink();
        Verse.AI.Job? ConsumePendingJob();
        void SetPendingJob(Verse.AI.Job job);
        bool RemoveGoal(string goalDescription);
        void RecordBehavior(BehaviorRecordDto record);
        void Cleanup();
        void ResubscribeEvents();
        void SwitchMode(AgentModeId modeId);
    }
}
