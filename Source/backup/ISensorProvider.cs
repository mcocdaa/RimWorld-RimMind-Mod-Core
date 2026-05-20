using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Extension;
using Verse;

namespace RimMind.Presentation.Settings
{
    public class AgentToolDefinition
    {
        public string Name = "";
        public string Description = "";
        public string? Parameters;
    }

    public interface ISensorProvider : IExtension
    {
        string SensorId { get; }
        float Priority { get; }
        int TickInterval { get; }
        string? Sense(Pawn pawn);
        List<AgentToolDefinition> GetAgentTools(Pawn pawn);
    }
}
