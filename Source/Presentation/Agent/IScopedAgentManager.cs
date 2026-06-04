using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;

namespace RimMind.Presentation.Agent
{
    public interface IScopedAgentManager
    {
        IScopedAgent GetOrCreate(string scopeType, string scopeId, IAgentBus agentBus, int? mapId = null);
        IScopedAgent? Find(string scopeType, string scopeId);
        IReadOnlyList<IScopedAgent> GetAll();
        bool Remove(string scopeType, string scopeId);
        void Clear();
    }
}
