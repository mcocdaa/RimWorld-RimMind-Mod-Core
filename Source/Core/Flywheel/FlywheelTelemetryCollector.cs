using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using RimMind.Core.Context;
using RimMind.Core.Settings;
using Verse;

namespace RimMind.Core.Flywheel
{
    public class TelemetryRecord
    {
        public string NpcId = null!;
        public string Scenario = null!;
        public int PromptTokens;
        public int CompletionTokens;
        public int TotalTokens;
        public int CachedTokens;
        public float BudgetValue;
        public string[] KeysIncluded = null!;
        public string[] KeysTrimmed = null!;
        public Dictionary<string, int> LayerTokenBreakdown = null!;
        public long TimestampTicks;
        public Dictionary<string, int>? KeyChangeFreq;
        public Dictionary<string, float>? CacheHitRate;
        public Dictionary<string, float>? ScoreDistribution;
        public int DiffCount;
        public float DiffMergeLatency;
        public bool ResponseParseSuccess;
        public Dictionary<string, long>? LatencyByLayerMs;
        public long RequestLatencyMs;
    }

    public class FlywheelTelemetryCollector
    {
        private const int RecentRecordsCapacity = 200;
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);

        private readonly ConcurrentQueue<string> _pendingWrites = new ConcurrentQueue<string>();
        private readonly List<TelemetryRecord> _recentRecords = new List<TelemetryRecord>();
        private readonly object _lock = new object();
        private readonly object _fileLock = new object();
        private readonly Timer _flushTimer;

        public FlywheelTelemetryCollector()
        {
            _flushTimer = new Timer(_ => FlushInternal(), null, FlushInterval, FlushInterval);
        }

        public void Record(TelemetryRecord record)
        {
            lock (_lock)
            {
                _recentRecords.Add(record);
                if (_recentRecords.Count > RecentRecordsCapacity)
                    _recentRecords.RemoveRange(0, _recentRecords.Count - RecentRecordsCapacity);
            }
            _pendingWrites.Enqueue(JsonConvert.SerializeObject(record, Formatting.None));
        }

        public List<TelemetryRecord> GetRecentRecords(int count)
        {
            lock (_lock)
            {
                if (count >= _recentRecords.Count)
                    return new List<TelemetryRecord>(_recentRecords);
                return _recentRecords.GetRange(_recentRecords.Count - count, count);
            }
        }

        public void Flush()
        {
            FlushInternal();
        }

        private void FlushInternal()
        {
            var lines = new List<string>();
            while (_pendingWrites.TryDequeue(out var line))
                lines.Add(line);

            if (lines.Count == 0) return;

            lock (_fileLock)
            {
                try
                {
                    string? settingsPath = RimMindCoreMod.Settings?.telemetryDataPath;
                    string dir = !string.IsNullOrWhiteSpace(settingsPath)
                        ? settingsPath!
                        : Path.Combine(GenFilePaths.SaveDataFolderPath, "Telemetry");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    string fileName = $"{DateTime.Now:yyyy-MM-dd}.jsonl";
                    string filePath = Path.Combine(dir, fileName);

                    File.AppendAllLines(filePath, lines);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimMind-Core] Telemetry flush failed: {ex.Message}");
                }
            }
        }

        private static Dictionary<string, float>? ComputeCacheHitRates(ContextSnapshot snapshot)
        {
            if (snapshot.CacheHitEvents.Count == 0) return null;
            var byLayer = new Dictionary<string, List<bool>>();
            foreach (var kvp in snapshot.CacheHitEvents)
            {
                string layer = kvp.Key.StartsWith("L0") ? "L0"
                    : kvp.Key.StartsWith("L1") ? "L1"
                    : kvp.Key.StartsWith("L2") ? "L2"
                    : kvp.Key.StartsWith("L3") ? "L3" : "L4";
                if (!byLayer.ContainsKey(layer))
                    byLayer[layer] = new List<bool>();
                byLayer[layer].Add(kvp.Value);
            }
            var rates = new Dictionary<string, float>();
            foreach (var kvp in byLayer)
            {
                if (kvp.Value.Count > 0)
                    rates[kvp.Key] = (float)kvp.Value.Count(v => v) / kvp.Value.Count;
            }
            return rates.Count > 0 ? rates : null;
        }
    }
}
