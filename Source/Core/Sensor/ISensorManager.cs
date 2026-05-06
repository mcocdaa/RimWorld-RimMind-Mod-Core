using System.Collections.Generic;
using RimMind.Core.Client;
using Verse;

namespace RimMind.Core.Sensor
{
    public interface ISensorManager
    {
        List<StructuredTool> BuildAgentTools(Pawn pawn);
        void RegisterSensorContextKeys();
    }
}
