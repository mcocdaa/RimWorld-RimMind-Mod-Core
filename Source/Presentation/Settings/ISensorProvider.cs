using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Extension;
using Verse;

namespace RimMind.Presentation.Settings
{
    /// <summary>
    /// [DEAD CODE] No implementations of ISensorProvider exist in the project.
    /// Backup copy at Source/backup/ISensorProvider.cs.
    /// Kept in place because SensorManager, RimMindRuntime, and RimMindAPI.Sensors
    /// reference this interface as part of the public API surface.
    /// </summary>
    [Obsolete("No implementations exist. This interface and AgentToolDefinition are dead code.")]
    public class AgentToolDefinition
    {
        public string Name = "";
        public string Description = "";
        public string? Parameters;
    }

    /// <summary>
    /// [DEAD CODE] No implementations of ISensorProvider exist in the project.
    /// Backup copy at Source/backup/ISensorProvider.cs.
    /// Kept in place because SensorManager, RimMindRuntime, and RimMindAPI.Sensors
    /// reference this interface as part of the public API surface.
    /// </summary>
    [Obsolete("No implementations exist. This interface is dead code.")]
    public interface ISensorProvider : IExtension
    {
        string SensorId { get; }
        float Priority { get; }
        int TickInterval { get; }
        string? Sense(Pawn pawn);
        List<AgentToolDefinition> GetAgentTools(Pawn pawn);
    }
}
