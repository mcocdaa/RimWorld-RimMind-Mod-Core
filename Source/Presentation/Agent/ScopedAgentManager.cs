using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models.Agent;

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
            var key = LegacyCompositeKey(scopeType, scopeId);
            if (_agents.TryGetValue(key, out var existing))
                return existing;
            var agent = _factory.Create(AgentScope.Custom(scopeType, scopeId, mapId), agentBus);
            _agents[key] = agent;
            return agent;
        }

        public IScopedAgent GetOrCreate(AgentScope scope, IAgentBus agentBus)
        {
            var key = scope.CompositeKey;
            if (_agents.TryGetValue(key, out var existing))
                return existing;
            var agent = _factory.Create(scope, agentBus);
            _agents[key] = agent;
            return agent;
        }

        public IScopedAgent? Find(string scopeType, string scopeId)
        {
            var key = LegacyCompositeKey(scopeType, scopeId);
            return _agents.TryGetValue(key, out var agent) ? agent : null;
        }

        public IScopedAgent? Find(AgentScope scope)
        {
            var key = scope.CompositeKey;
            return _agents.TryGetValue(key, out var agent) ? agent : null;
        }

        public IReadOnlyList<IScopedAgent> GetAll()
        {
            var result = new List<IScopedAgent>(_agents.Values);
            return result.AsReadOnly();
        }

        public bool Remove(string scopeType, string scopeId)
        {
            var key = LegacyCompositeKey(scopeType, scopeId);
            return RemoveByKey(key);
        }

        public bool Remove(AgentScope scope)
        {
            return RemoveByKey(scope.CompositeKey);
        }

        private bool RemoveByKey(string key)
        {
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

        private static string LegacyCompositeKey(string scopeType, string scopeId)
            => (string.IsNullOrWhiteSpace(scopeType) ? "unknown" : scopeType)
                + ":"
                + (string.IsNullOrWhiteSpace(scopeId) ? "unknown" : scopeId);
    }
}
