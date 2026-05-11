using System.Collections.Generic;

namespace RimMind.Contracts.Tools
{
    public interface IToolRegistry
    {
        void Register(IToolHandler handler);
        bool Unregister(string toolId);
        IToolHandler? FindById(string toolId);
        IReadOnlyList<IToolHandler> All { get; }
        IReadOnlyList<ToolDefinition> GetAllDefinitions();
    }
}
