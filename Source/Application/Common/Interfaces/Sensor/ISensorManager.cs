using System.Collections.Generic;
using RimMind.Application.Common.Models.Client;

namespace RimMind.Application.Common.Interfaces.Sensor
{
    public interface ISensorManager
    {
        List<StructuredTool> BuildAgentTools(object pawn);
        void RegisterSensorContextKeys();
    }
}
