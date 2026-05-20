using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;

namespace RimMind.Application.Common.Interfaces.Agent
{
    public interface IAgentControl : IAgentInfo, IJobProvider
    {
        bool IsActive { get; }
        AgentModeId CurrentModeId { get; }
        IAgentMode CurrentMode { get; }
        bool IsPawnValid { get; }
        string GetDebugInfo();

        void Tick();
        bool TransitionTo(AgentState newState);
        void ForceThink();
        void SwitchMode(AgentModeId modeId);
        bool RemoveGoal(string goalDescription);
        void RecordBehavior(BehaviorRecordDto record);
        void Cleanup();
        void ResubscribeEvents();
    }
}
