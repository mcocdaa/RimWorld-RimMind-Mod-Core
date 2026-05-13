using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Flywheel;

namespace RimMind.Application.Features.Flywheel
{
    internal sealed class FlywheelTelemetryCollector
    {
        private readonly ConcurrentQueue<TelemetryRecord> _records
            = new ConcurrentQueue<TelemetryRecord>();
        private readonly ILogSink? _log;
        private const int MaxRecords = 1000;

        public FlywheelTelemetryCollector(ILogSink? log = null) { _log = log; }

        public void Record(string metric, float value, Dictionary<string, string>? tags = null)
        {
            var record = new TelemetryRecord
            {
                Metric = metric,
                Value = value,
                TimestampTicks = DateTime.UtcNow.Ticks,
                Tags = tags
            };
            _records.Enqueue(record);
            while (_records.Count > MaxRecords)
                _records.TryDequeue(out _);
        }

        public List<TelemetryRecord> GetRecent(int count = 100)
        {
            var result = new List<TelemetryRecord>();
            foreach (var r in _records)
            {
                result.Add(r);
                if (result.Count >= count) break;
            }
            return result;
        }

        public Dictionary<string, float> ComputeAggregates()
        {
            var aggregates = new Dictionary<string, float>();
            var byMetric = new Dictionary<string, List<float>>();
            foreach (var r in _records)
            {
                if (!byMetric.ContainsKey(r.Metric))
                    byMetric[r.Metric] = new List<float>();
                byMetric[r.Metric].Add(r.Value);
            }
            foreach (var kvp in byMetric)
            {
                if (kvp.Value.Count > 0)
                    aggregates[$"{kvp.Key}_avg"] = kvp.Value.Count > 0 ? kvp.Value[kvp.Value.Count - 1] : 0f;
            }
            return aggregates;
        }

        public void Clear()
        {
            while (_records.TryDequeue(out _)) { }
        }
    }

    public class TelemetryRecord
    {
        public string Metric { get; set; } = "";
        public float Value { get; set; }
        public long TimestampTicks { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }
}
