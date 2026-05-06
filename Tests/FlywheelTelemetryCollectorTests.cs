using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using RimMind.Kernel.Flywheel;
using Xunit;

namespace RimMind.Core.Tests
{
    public class FlywheelTelemetryCollectorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FlywheelTelemetryCollector _collector;

        public FlywheelTelemetryCollectorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"RimMindTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            RimMindCoreMod.Settings = new AICoreSettings { telemetryDataPath = _tempDir };
            _collector = new FlywheelTelemetryCollector();
        }

        public void Dispose()
        {
            _collector.Dispose();
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch { }
            RimMindCoreMod.Settings = null;
        }

        private static TelemetryRecord MakeRecord(string npcId = "test", string scenario = "Decision")
        {
            return new TelemetryRecord
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
            _collector.Record(MakeRecord("npc1"));
            var recent = _collector.GetRecentRecords(10);
            Assert.Single(recent);
            Assert.Equal("npc1", recent[0].NpcId);
        }

        [Fact]
        public void Record_MultipleRecords_AllInRecentRecords()
        {
            _collector.Record(MakeRecord("npc1"));
            _collector.Record(MakeRecord("npc2"));
            _collector.Record(MakeRecord("npc3"));
            var recent = _collector.GetRecentRecords(10);
            Assert.Equal(3, recent.Count);
        }

        [Fact]
        public void GetRecentRecords_LimitedByCount()
        {
            for (int i = 0; i < 5; i++)
                _collector.Record(MakeRecord($"npc{i}"));
            var recent = _collector.GetRecentRecords(3);
            Assert.Equal(3, recent.Count);
            Assert.Equal("npc2", recent[0].NpcId);
            Assert.Equal("npc3", recent[1].NpcId);
            Assert.Equal("npc4", recent[2].NpcId);
        }

        [Fact]
        public void GetRecentRecords_CountExceedsTotal_ReturnsAll()
        {
            _collector.Record(MakeRecord("a"));
            _collector.Record(MakeRecord("b"));
            var recent = _collector.GetRecentRecords(100);
            Assert.Equal(2, recent.Count);
        }

        [Fact]
        public void Record_CapacityOverflow_TrimsOld()
        {
            for (int i = 0; i < 205; i++)
                _collector.Record(MakeRecord($"npc{i}"));
            var recent = _collector.GetRecentRecords(300);
            Assert.Equal(200, recent.Count);
            Assert.Equal("npc5", recent[0].NpcId);
        }

        [Fact]
        public void GetRecentRecords_Empty_ReturnsEmptyList()
        {
            var recent = _collector.GetRecentRecords(10);
            Assert.Empty(recent);
        }

        [Fact]
        public void Flush_WritesPendingRecordsToFile()
        {
            _collector.Record(MakeRecord("npc1"));
            _collector.Record(MakeRecord("npc2"));
            _collector.Flush();
            var files = Directory.GetFiles(_tempDir, "*.jsonl");
            Assert.Single(files);
            var lines = File.ReadAllLines(files[0]);
            Assert.Equal(2, lines.Length);
        }

        [Fact]
        public void Flush_EmptyCollector_DoesNotCreateFile()
        {
            _collector.Flush();
            var files = Directory.GetFiles(_tempDir, "*.jsonl");
            Assert.Empty(files);
        }

        [Fact]
        public void ImplementsIDisposable()
        {
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(FlywheelTelemetryCollector)));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            _collector.Dispose();
            _collector.Dispose();
        }

        [Fact]
        public void Record_FromMultipleThreads_AllRecorded()
        {
            var threads = new Thread[10];
            for (int t = 0; t < threads.Length; t++)
            {
                int idx = t;
                threads[t] = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                        _collector.Record(MakeRecord($"thread{idx}_npc{i}"));
                });
            }
            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();
            var recent = _collector.GetRecentRecords(200);
            Assert.Equal(100, recent.Count);
        }
    }
}
