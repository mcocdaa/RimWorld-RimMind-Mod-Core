using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using RimMind.Core.Flywheel;
using Xunit;

namespace RimMind.Core.Tests
{
    public class TelemetryRecord
    {
        public string NpcId = "";
        public string Scenario = "";
        public int TotalTokens;
        public int CompletionTokens;
        public int CachedTokens;
        public float BudgetValue;
        public string[] KeysIncluded = System.Array.Empty<string>();
        public string[] KeysTrimmed = System.Array.Empty<string>();
        public Dictionary<string, float>? CacheHitRate;
        public bool ResponseParseSuccess;
    }

    public class SimpleTelemetryBuffer
    {
        private const int RecentRecordsCapacity = 200;
        private readonly List<TelemetryRecord> _recentRecords = new List<TelemetryRecord>();

        public void Record(TelemetryRecord record)
        {
            _recentRecords.Add(record);
            if (_recentRecords.Count > RecentRecordsCapacity)
                _recentRecords.RemoveRange(0, _recentRecords.Count - RecentRecordsCapacity);
        }

        public List<TelemetryRecord> GetRecentRecords(int count)
        {
            if (count >= _recentRecords.Count)
                return new List<TelemetryRecord>(_recentRecords);
            return _recentRecords.GetRange(_recentRecords.Count - count, count);
        }
    }

    public class FlywheelTelemetryCollectorTests
    {
        private static TelemetryRecord MakeRecord(string npcId = "test", string scenario = "Decision")
        {
            return new TelemetryRecord
            {
                NpcId = npcId,
                Scenario = scenario,
                TotalTokens = 100,
                BudgetValue = 1.0f,
                ResponseParseSuccess = true,
                KeysIncluded = new string[5],
                KeysTrimmed = new string[1],
            };
        }

        [Fact]
        public void Record_AddsToRecentRecords()
        {
            var buffer = new SimpleTelemetryBuffer();
            var record = MakeRecord();
            buffer.Record(record);

            var recent = buffer.GetRecentRecords(10);
            Assert.Single(recent);
            Assert.Equal("test", recent[0].NpcId);
        }

        [Fact]
        public void Record_MultipleRecords_AllAccessible()
        {
            var buffer = new SimpleTelemetryBuffer();
            buffer.Record(MakeRecord("npc1"));
            buffer.Record(MakeRecord("npc2"));
            buffer.Record(MakeRecord("npc3"));

            var recent = buffer.GetRecentRecords(10);
            Assert.Equal(3, recent.Count);
        }

        [Fact]
        public void GetRecentRecords_LimitedByCount()
        {
            var buffer = new SimpleTelemetryBuffer();
            for (int i = 0; i < 5; i++)
                buffer.Record(MakeRecord($"npc{i}"));

            var recent = buffer.GetRecentRecords(3);
            Assert.Equal(3, recent.Count);
            Assert.Equal("npc2", recent[0].NpcId);
            Assert.Equal("npc3", recent[1].NpcId);
            Assert.Equal("npc4", recent[2].NpcId);
        }

        [Fact]
        public void GetRecentRecords_CountExceedsTotal_ReturnsAll()
        {
            var buffer = new SimpleTelemetryBuffer();
            buffer.Record(MakeRecord("a"));
            buffer.Record(MakeRecord("b"));

            var recent = buffer.GetRecentRecords(100);
            Assert.Equal(2, recent.Count);
        }

        [Fact]
        public void Record_CapacityOverflow_TrimsOld()
        {
            var buffer = new SimpleTelemetryBuffer();
            for (int i = 0; i < 205; i++)
                buffer.Record(MakeRecord($"npc{i}"));

            var recent = buffer.GetRecentRecords(300);
            Assert.Equal(200, recent.Count);
            Assert.Equal("npc5", recent[0].NpcId);
        }

        [Fact]
        public void GetRecentRecords_Empty_ReturnsEmptyList()
        {
            var buffer = new SimpleTelemetryBuffer();
            var recent = buffer.GetRecentRecords(10);
            Assert.Empty(recent);
        }

        [Fact]
        public void GetRecentRecords_ReturnsCopy()
        {
            var buffer = new SimpleTelemetryBuffer();
            buffer.Record(MakeRecord());

            var recent1 = buffer.GetRecentRecords(10);
            var recent2 = buffer.GetRecentRecords(10);
            Assert.NotSame(recent1, recent2);
        }
    }

    public class FlywheelTelemetryCollectorIntegrationTests : IDisposable
    {
        private readonly string _tempDir;

        public FlywheelTelemetryCollectorIntegrationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"RimMindTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            RimMindCoreMod.Settings = new AICoreSettings { telemetryDataPath = _tempDir };
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch { }
            RimMindCoreMod.Settings = null;
        }

        private static RimMind.Core.Flywheel.TelemetryRecord MakeFlywheelRecord(string npcId = "test", string scenario = "Decision")
        {
            return new RimMind.Core.Flywheel.TelemetryRecord
            {
                NpcId = npcId,
                Scenario = scenario,
                TotalTokens = 100,
                BudgetValue = 1.0f,
                ResponseParseSuccess = true,
                KeysIncluded = Array.Empty<string>(),
                KeysTrimmed = Array.Empty<string>(),
                LayerTokenBreakdown = new Dictionary<string, int>(),
            };
        }

        [Fact]
        public void Record_AddsToRecentRecords()
        {
            var collector = new FlywheelTelemetryCollector();
            collector.Record(MakeFlywheelRecord("npc1"));

            var recent = collector.GetRecentRecords(10);
            Assert.Single(recent);
            Assert.Equal("npc1", recent[0].NpcId);
        }

        [Fact]
        public void Record_MultipleRecords_AllInRecentRecords()
        {
            var collector = new FlywheelTelemetryCollector();
            collector.Record(MakeFlywheelRecord("npc1"));
            collector.Record(MakeFlywheelRecord("npc2"));
            collector.Record(MakeFlywheelRecord("npc3"));

            var recent = collector.GetRecentRecords(10);
            Assert.Equal(3, recent.Count);
        }

        [Fact]
        public void GetRecentRecords_LimitedByCount()
        {
            var collector = new FlywheelTelemetryCollector();
            for (int i = 0; i < 5; i++)
                collector.Record(MakeFlywheelRecord($"npc{i}"));

            var recent = collector.GetRecentRecords(3);
            Assert.Equal(3, recent.Count);
            Assert.Equal("npc2", recent[0].NpcId);
            Assert.Equal("npc3", recent[1].NpcId);
            Assert.Equal("npc4", recent[2].NpcId);
        }

        [Fact]
        public void Flush_WritesPendingRecordsToFile()
        {
            var collector = new FlywheelTelemetryCollector();
            collector.Record(MakeFlywheelRecord("npc1"));
            collector.Record(MakeFlywheelRecord("npc2"));

            collector.Flush();

            var files = Directory.GetFiles(_tempDir, "*.jsonl");
            Assert.Single(files);
            var lines = File.ReadAllLines(files[0]);
            Assert.Equal(2, lines.Length);
            Assert.Contains("npc1", lines[0]);
            Assert.Contains("npc2", lines[1]);
        }

        [Fact]
        public void Flush_EmptyCollector_DoesNotCreateFile()
        {
            var collector = new FlywheelTelemetryCollector();
            collector.Flush();

            var files = Directory.GetFiles(_tempDir, "*.jsonl");
            Assert.Empty(files);
        }

        [Fact]
        public void Flush_DrainsPendingWrites_SecondFlushIsEmpty()
        {
            var collector = new FlywheelTelemetryCollector();
            collector.Record(MakeFlywheelRecord("npc1"));

            collector.Flush();

            var files = Directory.GetFiles(_tempDir, "*.jsonl");
            Assert.Single(files);
            var lines1 = File.ReadAllLines(files[0]);
            Assert.Single(lines1);

            collector.Flush();

            var lines2 = File.ReadAllLines(files[0]);
            Assert.Single(lines2);
        }

        [Fact]
        public void Record_CapacityOverflow_TrimsOld()
        {
            var collector = new FlywheelTelemetryCollector();
            for (int i = 0; i < 205; i++)
                collector.Record(MakeFlywheelRecord($"npc{i}"));

            var recent = collector.GetRecentRecords(300);
            Assert.Equal(200, recent.Count);
            Assert.Equal("npc5", recent[0].NpcId);
        }

        [Fact]
        public void Record_FromMultipleThreads_AllRecorded()
        {
            var collector = new FlywheelTelemetryCollector();
            var threads = new Thread[10];
            for (int t = 0; t < threads.Length; t++)
            {
                int idx = t;
                threads[t] = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                        collector.Record(MakeFlywheelRecord($"thread{idx}_npc{i}"));
                });
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            var recent = collector.GetRecentRecords(200);
            Assert.Equal(100, recent.Count);

            collector.Flush();
            var files = Directory.GetFiles(_tempDir, "*.jsonl");
            Assert.Single(files);
            var lines = File.ReadAllLines(files[0]);
            Assert.Equal(100, lines.Length);
        }
    }
}
