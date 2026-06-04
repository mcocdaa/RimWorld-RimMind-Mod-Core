using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;

namespace RimMind.Presentation.Agent
{
    public sealed class ScopedAgentManager : IScopedAgentManager
    {
        private readonly IScopedAgentFactory _factory;
        private readonly Dictionary<string, IScopedAgent> _agents = new();

        public ScopedAgentManager(IScopedAgentFactory factory)
        {
            _factory = factory;
        }

        public IScopedAgent GetOrCreate(string scopeType, string scopeId, IAgentBus agentBus, int? mapId = null)
        {
            var key = CompositeKey(scopeType, scopeId);
            if (_agents.TryGetValue(key, out var existing))
                return existing;
            var agent = _factory.Create(scopeType, scopeId, agentBus, mapId);
            _agents[key] = agent;
            return agent;
        }

        public IScopedAgent? Find(string scopeType, string scopeId)
        {
            var key = CompositeKey(scopeType, scopeId);
            return _agents.TryGetValue(key, out var agent) ? agent : null;
        }

        public IReadOnlyList<IScopedAgent> GetAll()
        {
            var result = new List<IScopedAgent>(_agents.Values);
            return result.AsReadOnly();
        }

        public bool Remove(string scopeType, string scopeId)
        {
            var key = CompositeKey(scopeType, scopeId);
            if (_agents.TryGetValue(key, out var agent))
            {
                agent.Cleanup();
                agent.Destroy();
                _agents.Remove(key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var agent in _agents.Values)
            {
                agent.Cleanup();
                agent.Destroy();
            }
            _agents.Clear();
        }

        private static string CompositeKey(string scopeType, string scopeId)
            => $"{scopeType}:{scopeId}";
    }
}
