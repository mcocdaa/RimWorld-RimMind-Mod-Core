using RimMind.Application.Common.Interfaces;

namespace RimMind.Presentation.Agent
{
    public interface IScopedAgentFactory
    {
        IScopedAgent Create(string scopeType, string scopeId, IAgentBus agentBus, int? mapId = null);
    }
}
