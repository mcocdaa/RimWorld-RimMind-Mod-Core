using System.Collections.Generic;
using RimMind.Core.Client;
using Verse;

namespace RimMind.Core.Extensions
{
    public interface IAgentActionBridge
    {
        bool CanExecute(Pawn pawn, string action);
        void Execute(Pawn pawn, string action, string? targetName = null);
        List<StructuredTool>? GetAvailableTools(Pawn pawn);
    }
}
