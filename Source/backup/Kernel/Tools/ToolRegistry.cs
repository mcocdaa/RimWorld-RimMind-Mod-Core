using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Tools;

namespace RimMind.Kernel.Tools
{
    public sealed class ToolRegistry : IToolRegistry
    {
        private readonly ConcurrentDictionary<string, IToolHandler> _handlers = new();

        public void Register(IToolHandler handler)
        {
            if (handler == null) return;
            _handlers[handler.Definition.Id] = handler;
        }

        public bool Unregister(string toolId)
        {
            return _handlers.TryRemove(toolId, out _);
        }

        public IToolHandler? FindById(string toolId)
        {
            return _handlers.TryGetValue(toolId, out var handler) ? handler : null;
        }

        public IReadOnlyList<IToolHandler> All => _handlers.Values.ToList().AsReadOnly();

        public IReadOnlyList<ToolDefinition> GetAllDefinitions()
        {
            return _handlers.Values.Select(h => h.Definition).ToList().AsReadOnly();
        }
    }
}
