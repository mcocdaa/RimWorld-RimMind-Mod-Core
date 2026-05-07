using System.Collections.Generic;
using Verse;

namespace RimMind.Core.Sensor
{
    public class AgentToolDefinition
    {
        public string Name = "";
        public string Description = "";
        public string? Parameters;
    }

    public interface ISensorProvider
    {
        string SensorId { get; }
        float Priority { get; }
        int TickInterval { get; }
        string? Sense(Pawn pawn);
        List<AgentToolDefinition> GetAgentTools(Pawn pawn);
    }
}
