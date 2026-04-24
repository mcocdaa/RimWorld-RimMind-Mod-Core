using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RimMind.Core.Context;
using Verse;

namespace RimMind.Core.Flywheel
{
    public class TelemetryRecord
    {
        public string NpcId;
        public string Scenario;
        public int PromptTokens;
        public int CompletionTokens;
        public int TotalTokens;
        public int CachedTokens;
        public float BudgetValue;
        public string[] KeysIncluded;
        public string[] KeysTrimmed;
        public Dictionary<string, int> LayerTokenBreakdown;
        public long TimestampTicks;
    }

    public class FlywheelTelemetryCollector
    {
        private const int FlushThreshold = 50;

        private readonly List<TelemetryRecord> _buffer = new List<TelemetryRecord>();
        private readonly object _lock = new object();

        public void Record(TelemetryRecord record)
        {
            lock (_lock)
            {
                _buffer.Add(record);
                if (_buffer.Count >= FlushThreshold)
                    FlushInternal();
            }
        }

        public void Flush()
        {
            lock (_lock)
            {
                FlushInternal();
            }
        }

        private void FlushInternal()
        {
            if (_buffer.Count == 0) return;

            var toWrite = new List<TelemetryRecord>(_buffer);
            _buffer.Clear();

            try
            {
                string dataDir = Path.GetDirectoryName(GenFilePaths.SaveFolder) ?? GenFilePaths.SaveFolder;
                string dir = Path.Combine(dataDir, "Telemetry");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string fileName = $"{DateTime.Now:yyyy-MM-dd}.jsonl";
                string filePath = Path.Combine(dir, fileName);

                var lines = new List<string>(toWrite.Count);
                foreach (var record in toWrite)
                    lines.Add(JsonConvert.SerializeObject(record, Formatting.None));

                File.AppendAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimMind] Telemetry flush failed: {ex.Message}");
            }
        }

        public void RecordSnapshotBuild(ContextSnapshot snapshot, float budgetValue, string[] includedKeys, string[] trimmedKeys)
        {
            var record = new TelemetryRecord
            {
                NpcId = snapshot.NpcId,
                Scenario = snapshot.Scenario,
                BudgetValue = budgetValue,
                KeysIncluded = includedKeys ?? new string[0],
                KeysTrimmed = trimmedKeys ?? new string[0],
                LayerTokenBreakdown = new Dictionary<string, int>
                {
                    { "L0", snapshot.Meta.L0Tokens },
                    { "L1", snapshot.Meta.L1Tokens },
                    { "L2", snapshot.Meta.L2Tokens },
                    { "L3", snapshot.Meta.L3Tokens },
                    { "L4", snapshot.Meta.L4Tokens },
                },
                TimestampTicks = DateTime.Now.Ticks,
            };
            Record(record);
        }
    }
}
