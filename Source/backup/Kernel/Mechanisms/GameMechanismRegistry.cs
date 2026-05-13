using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Mechanisms;
using RimMind.Contracts.Tools;

namespace RimMind.Kernel.Mechanisms
{
    public sealed class GameMechanismRegistry : IGameMechanismRegistry
    {
        private readonly ConcurrentDictionary<string, IGameMechanism> _mechanisms = new();
        private readonly IToolRegistry? _toolRegistry;

        public GameMechanismRegistry(IToolRegistry? toolRegistry = null)
        {
            _toolRegistry = toolRegistry;
        }

        public void Register(IGameMechanism mechanism)
        {
            if (mechanism == null) return;
            _mechanisms[mechanism.MechanismId] = mechanism;

            if (_toolRegistry != null)
            {
                foreach (var op in mechanism.SupportedOperations)
                {
                    var handler = new MechanismToolHandler(mechanism, op);
                    _toolRegistry.Register(handler);
                }

                if (mechanism.SupportedOperations.Contains(MechanismOperationType.List))
                {
                    var listHandler = new MechanismListToolHandler(mechanism);
                    _toolRegistry.Register(listHandler);
                }
            }
        }

        public bool Unregister(string mechanismId)
        {
            if (!_mechanisms.TryRemove(mechanismId, out var mechanism)) return false;

            if (_toolRegistry != null)
            {
                foreach (var op in mechanism.SupportedOperations)
                {
                    var toolId = $"{mechanism.MechanismId}.{OperationSuffix(op)}";
                    _toolRegistry.Unregister(toolId);
                }

                var listToolId = $"{mechanismId}.list";
                _toolRegistry.Unregister(listToolId);
            }

            return true;
        }

        public IGameMechanism? FindById(string mechanismId)
        {
            return _mechanisms.TryGetValue(mechanismId, out var mechanism) ? mechanism : null;
        }

        public IReadOnlyList<IGameMechanism> All => _mechanisms.Values.ToList().AsReadOnly();

        private static string OperationSuffix(MechanismOperationType operation)
        {
            return operation switch
            {
                MechanismOperationType.Query => "query",
                MechanismOperationType.Set => "set",
                MechanismOperationType.Add => "add",
                MechanismOperationType.Remove => "remove",
                MechanismOperationType.Toggle => "toggle",
                MechanismOperationType.Trigger => "trigger",
                MechanismOperationType.List => "list",
                MechanismOperationType.Watch => "watch",
                _ => operation.ToString().ToLowerInvariant()
            };
        }

    }
}
