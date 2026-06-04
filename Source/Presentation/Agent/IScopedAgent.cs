using RimMind.Application.Common.Interfaces.Agent;

namespace RimMind.Presentation.Agent
{
    public interface IScopedAgent : IAgentControl
    {
        string ScopeId { get; }
        string ScopeType { get; }
        int? MapId { get; }
    }
}
