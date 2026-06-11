using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;

namespace RimMind.Presentation.Agent
{
    public sealed class ScopedAgentFactory : IScopedAgentFactory
    {
        public IScopedAgent Create(string scopeType, string scopeId, IAgentBus agentBus, int? mapId = null)
        {
            return new ScopedAgent(scopeId, scopeType, agentBus, mapId);
        }
    }
}
