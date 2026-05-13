using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Tools;

namespace RimMind.Application.Features.Tools
{
    internal sealed class ToolRegistry : IToolRegistry
    {
        private readonly ConcurrentDictionary<string, IToolHandler> _handlers
            = new ConcurrentDictionary<string, IToolHandler>();
        private readonly ILogSink? _log;

        public ToolRegistry(ILogSink? log = null) { _log = log; }

        public void Register(IToolHandler handler)
        {
            if (handler == null) return;
            _handlers[handler.Definition.Name] = handler;
        }

        public bool Unregister(string toolId)
        {
            return _handlers.TryRemove(toolId, out _);
        }

        public IToolHandler? FindById(string toolId)
        {
            return _handlers.TryGetValue(toolId, out var h) ? h : null;
        }

        public IReadOnlyList<IToolHandler> All => _handlers.Values.ToList();

        public IReadOnlyList<ToolDefinition> GetAllDefinitions()
        {
            return _handlers.Values.Select(h => h.Definition).ToList();
        }
    }
}
