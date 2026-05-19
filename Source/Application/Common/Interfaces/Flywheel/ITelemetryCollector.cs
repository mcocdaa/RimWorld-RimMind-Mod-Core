using System.Collections.Generic;
using RimMind.Application.Common.Models.Flywheel;

namespace RimMind.Application.Common.Interfaces.Flywheel
{
    public interface ITelemetryCollector
    {
        void Record(string metric, float value, Dictionary<string, string>? tags = null);
        List<TelemetryRecord> GetRecent(int count = 100);
        Dictionary<string, float> ComputeAggregates();
        void Clear();
        void Flush();
        List<TelemetryRecord> GetRecentRecords(int count = 100);
    }
}
