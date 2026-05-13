using RimMind.Contracts.Sensor;
using RimMind.Core.Runtime;
using System.Collections.Generic;

namespace RimMind.Core
{
    public static partial class RimMindAPI
    {
        public static class Sensors
        {
            public static void RegisterSensorProvider(ISensorProvider provider)
                => RimMindRuntime.Instance.RegisterSensorProvider(provider);

            public static void UnregisterSensorProvider(string sensorId)
                => RimMindRuntime.Instance.UnregisterSensorProvider(sensorId);

            public static IReadOnlyList<ISensorProvider> SensorProviders
                => RimMindRuntime.Instance.SensorProvidersList;
        }
    }
}
