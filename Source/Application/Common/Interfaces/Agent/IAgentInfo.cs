using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;

namespace RimMind.Application.Common.Interfaces.Agent
{
    public interface IAgentInfo
    {
        AgentState State { get; }
        string NpcId { get; }
        string Label { get; }
        int? LastThinkTick { get; set; }
        int GoalCount { get; }
    }
}
