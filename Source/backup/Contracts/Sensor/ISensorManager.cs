using System.Collections.Generic;
using RimMind.Contracts.Client;

namespace RimMind.Contracts.Sensor
{
    public interface ISensorManager
    {
        List<StructuredTool> BuildAgentTools(object pawn);
        void RegisterSensorContextKeys();
    }
}
