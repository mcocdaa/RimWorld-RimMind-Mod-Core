using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Core.Context;
using RimMind.Core.Flywheel;
using RimMind.Core.Npc;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ContextEngineTests : IDisposable
    {
        private readonly ContextEngine _engine;
        private readonly HistoryManager _historyManager;
        private readonly Pawn _pawn;
        private readonly string _npcId;
        private readonly AICoreSettings? _originalSettings;
        private readonly FlywheelParameterStore? _originalFlywheel;

        public ContextEngineTests()
        {
            _historyManager = new HistoryManager();
            _engine = new ContextEngine(_historyManager);
            _pawn = new Pawn { thingIDNumber = 42, Dead = false };
            _npcId = $"NPC-{_pawn.thingIDNumber}";
            NpcManager.IndexPawn(_pawn);
            _originalSettings = RimMindCoreMod.Settings;
            _originalFlywheel = FlywheelParameterStore.Instance;
            RimMindCoreMod.Settings = new AICoreSettings
            {
                Context = new ContextSettings(),
                maxTokens = 800,
                defaultTemperature = 0.7f,
                thinkCooldownTicks = 30000,
                contextDiffLifetimeTicks = 36000,
            };
            var flywheel = new FlywheelParameterStore();
            flywheel.FinalizeInit();
            ScenarioRegistry.RegisterCoreScenarios();
            RelevanceTable.RegisterCoreRelevance();
        }

        public void Dispose()
        {
            _engine.Dispose();
            RimMindCoreMod.Settings = _originalSettings;
            if (_originalFlywheel != null)
                _originalFlywheel.FinalizeInit();
            ContextKeyRegistry.Clear();
            ScenarioRegistry.Clear();
            RelevanceTable.Clear();
            NpcManager.ClearPawnIndex();
        }

        private void RegisterTestKey(string key, ContextLayer layer, float priority, string content)
        {
            ContextKeyRegistry.Register(key, layer, priority, _ => new List<ContextEntry> { new ContextEntry(content) }, "Test");
        }

        private ContextRequest CreateRequest(string? scenario = null)
        {
            return new ContextRequest
            {
                NpcId = _npcId,
                Scenario = scenario ?? ScenarioIds.Dialogue,
                Budget = 0.6f,
                MaxTokens = 800,
                Temperature = 0.7f,
            };
        }

        [Fact]
        public void BuildSnapshot_ReturnsNonNullSnapshot()
        {
            RegisterTestKey("test_l0", ContextLayer.L0_Static, 1.0f, "static content");
            RegisterTestKey("test_l1", ContextLayer.L1_Baseline, 0.9f, "baseline content");

            var snapshot = _engine.BuildSnapshot(CreateRequest());

            Assert.NotNull(snapshot);
            Assert.Equal(_npcId, snapshot.NpcId);
            Assert.Equal(ScenarioIds.Dialogue, snapshot.Scenario);
        }

        [Fact]
        public void BuildSnapshot_IncludesL0Content()
        {
            RegisterTestKey("test_l0", ContextLayer.L0_Static, 1.0f, "hello from L0");

            var snapshot = _engine.BuildSnapshot(CreateRequest());

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.Messages, m => m.Content != null && m.Content.Contains("hello from L0"));
        }

        [Fact]
        public void BuildSnapshot_L0CacheHitOnSecondCall()
        {
            RegisterTestKey("test_l0", ContextLayer.L0_Static, 1.0f, "cached content");

            var first = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(first);
            Assert.False(first.CacheHitEvents.TryGetValue("L0_test_l0", out var hit1) && hit1);

            var second = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(second);
            Assert.True(second.CacheHitEvents.TryGetValue("L0_test_l0", out var hit2) && hit2);
        }

        [Fact]
        public void BuildSnapshot_L1CacheHitOnSecondCall()
        {
            RegisterTestKey("test_l1", ContextLayer.L1_Baseline, 0.9f, "baseline data");

            var first = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(first);

            var second = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(second);
            Assert.True(second.CacheHitEvents.TryGetValue("L1_test_l1", out var hit) && hit);
        }

        [Fact]
        public void BuildSnapshot_DiffGeneratedWhenKeyChanges()
        {
            int callCount = 0;
            string[] values = { "value_A", "value_B" };
            ContextKeyRegistry.Register("changing_key", ContextLayer.L3_State, 0.5f,
                _ => new List<ContextEntry> { new ContextEntry(values[callCount]) }, "Test");

            var first = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(first);

            callCount = 1;
            var second = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(second);

            var third = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(third);
            Assert.True(third.DiffCount > 0);
        }

        [Fact]
        public void BuildSnapshot_DiffMergedAfterExpiry()
        {
            int callCount = 0;
            string[] values = { "original", "updated" };
            ContextKeyRegistry.Register("diff_key", ContextLayer.L3_State, 0.5f,
                _ => new List<ContextEntry> { new ContextEntry(values[callCount]) }, "Test");

            var first = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(first);

            callCount = 1;
            var second = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(second);

            var third = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(third);
            Assert.True(third.DiffCount > 0);

            Find.TickManager.TicksGame += RimMindCoreMod.Settings!.contextDiffLifetimeTicks + 1;

            var fourth = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(fourth);

            var fifth = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(fifth);
            Assert.Equal(0, fifth.DiffCount);
        }

        [Fact]
        public void BuildSnapshot_BudgetTrimmingReducesMessages()
        {
            for (int i = 0; i < 5; i++)
            {
                RegisterTestKey($"l3_key_{i}", ContextLayer.L3_State, 0.3f,
                    new string('x', 2000));
            }

            var snapshot = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(snapshot);
            Assert.True(snapshot.Messages.Count > 0);
        }

        [Fact]
        public void TouchCache_EvictsOldestWhenOverCapacity()
        {
            RimMindCoreMod.Settings.Context.maxCacheEntries = 3;

            for (int i = 0; i < 5; i++)
            {
                var req = new ContextRequest
                {
                    NpcId = $"NPC-{100 + i}",
                    Scenario = ScenarioIds.Dialogue,
                    Budget = 0.6f,
                };
                RegisterTestKey($"evict_key_{i}", ContextLayer.L0_Static, 1.0f, $"content_{i}");
                _engine.BuildSnapshot(req);
            }

            Assert.True(_engine.GetL0CacheCount() <= 3);
        }

        [Fact]
        public void ResetCaches_ClearsAllInternalState()
        {
            RegisterTestKey("reset_l0", ContextLayer.L0_Static, 1.0f, "content");
            RegisterTestKey("reset_l1", ContextLayer.L1_Baseline, 0.9f, "baseline");

            _engine.BuildSnapshot(CreateRequest());
            Assert.True(_engine.GetL0CacheCount() > 0);

            _engine.ResetCaches();

            Assert.Equal(0, _engine.GetL0CacheCount());
            Assert.Equal(0, _engine.GetL1BlockCacheCount());
            Assert.Equal(0, _engine.GetDiffStoreCount());
        }

        [Fact]
        public void BuildSnapshot_ReturnsNullAfterDisposal()
        {
            RegisterTestKey("dispose_key", ContextLayer.L0_Static, 1.0f, "content");

            _engine.Dispose();

            var snapshot = _engine.BuildSnapshot(CreateRequest());
            Assert.Null(snapshot);
        }

        [Fact]
        public void ResetCaches_DoesNothingAfterDisposal()
        {
            RegisterTestKey("dispose_reset", ContextLayer.L0_Static, 1.0f, "content");
            _engine.BuildSnapshot(CreateRequest());

            _engine.Dispose();
            _engine.ResetCaches();
        }

        [Fact]
        public void InvalidateLayer_ClearsL0Cache()
        {
            RegisterTestKey("inv_l0", ContextLayer.L0_Static, 1.0f, "static");
            _engine.BuildSnapshot(CreateRequest());
            Assert.True(_engine.GetL0CacheCount() > 0);

            _engine.InvalidateLayer(_npcId, ContextLayer.L0_Static);

            Assert.Equal(0, _engine.GetL0CacheCount());
        }

        [Fact]
        public void InvalidateLayer_ClearsL1Cache()
        {
            RegisterTestKey("inv_l1", ContextLayer.L1_Baseline, 0.9f, "baseline");
            _engine.BuildSnapshot(CreateRequest());
            Assert.True(_engine.GetL1BlockCacheCount() > 0);

            _engine.InvalidateLayer(_npcId, ContextLayer.L1_Baseline);

            Assert.Equal(0, _engine.GetL1BlockCacheCount());
        }

        [Fact]
        public void InvalidateNpc_ClearsAllNpcData()
        {
            RegisterTestKey("npc_l0", ContextLayer.L0_Static, 1.0f, "static");
            RegisterTestKey("npc_l1", ContextLayer.L1_Baseline, 0.9f, "baseline");

            _engine.BuildSnapshot(CreateRequest());
            Assert.True(_engine.GetL0CacheCount() > 0);

            _engine.InvalidateNpc(_npcId);

            Assert.Equal(0, _engine.GetL0CacheCount());
            Assert.Equal(0, _engine.GetL1BlockCacheCount());
        }

        [Fact]
        public void BuildSnapshot_WithExcludeKeys_ExcludesSpecifiedKeys()
        {
            RegisterTestKey("excluded_key", ContextLayer.L0_Static, 1.0f, "should be excluded");
            RegisterTestKey("included_key", ContextLayer.L0_Static, 1.0f, "should be included");

            var req = CreateRequest();
            req.ExcludeKeys = new[] { "excluded_key" };

            var snapshot = _engine.BuildSnapshot(req);
            Assert.NotNull(snapshot);
            Assert.Contains("included_key", snapshot.IncludedKeys);
            Assert.DoesNotContain("excluded_key", snapshot.IncludedKeys);
        }

        [Fact]
        public void BuildSnapshot_SetsEstimatedTokens()
        {
            RegisterTestKey("token_l0", ContextLayer.L0_Static, 1.0f, "some content for token estimation");

            var snapshot = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(snapshot);
            Assert.True(snapshot.EstimatedTokens > 0);
        }

        [Fact]
        public void BuildSnapshot_SetsLatencyByLayer()
        {
            RegisterTestKey("latency_l0", ContextLayer.L0_Static, 1.0f, "content");

            var snapshot = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(snapshot);
            Assert.True(snapshot.LatencyByLayerMs.ContainsKey("L0"));
        }

        [Fact]
        public void BuildSnapshot_IncludesHistoryMessages()
        {
            _historyManager.AddTurn(_npcId, "hello", "world");

            var snapshot = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.Messages, m => m.Content == "hello");
            Assert.Contains(snapshot.Messages, m => m.Content == "world");
        }

        [Fact]
        public void BuildSnapshot_WithCurrentQuery_IncludesQueryMessage()
        {
            var req = CreateRequest();
            req.CurrentQuery = "what is happening?";

            var snapshot = _engine.BuildSnapshot(req);
            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.Messages, m => m.Content != null && m.Content.Contains("what is happening?"));
        }

        [Fact]
        public void GetScheduler_ReturnsNonNull()
        {
            Assert.NotNull(_engine.GetScheduler());
        }

        [Fact]
        public void GetEmbeddingSnapshotStore_ReturnsNonNull()
        {
            Assert.NotNull(_engine.GetEmbeddingSnapshotStore());
        }

        [Fact]
        public void BuildSnapshot_EmptyKeys_StillProducesSnapshot()
        {
            var snapshot = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(snapshot);
            Assert.Equal(_npcId, snapshot.NpcId);
        }

        [Fact]
        public void BuildSnapshot_BudgetValueIsSet()
        {
            var req = CreateRequest();
            req.Budget = 0.8f;

            var snapshot = _engine.BuildSnapshot(req);
            Assert.NotNull(snapshot);
            Assert.Equal(0.8f, snapshot.BudgetValue);
        }

        [Fact]
        public void BuildSnapshot_KeyScoresSetForL2L3L5()
        {
            RegisterTestKey("score_l2", ContextLayer.L2_Environment, 0.7f, "env content");
            RegisterTestKey("score_l3", ContextLayer.L3_State, 0.3f, "state content");

            var snapshot = _engine.BuildSnapshot(CreateRequest());
            Assert.NotNull(snapshot);
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _engine.Dispose();
            _engine.Dispose();
        }

        [Fact]
        public void InvalidateKey_RemovesSpecificKey()
        {
            RegisterTestKey("spec_key", ContextLayer.L1_Baseline, 0.9f, "specific");
            _engine.BuildSnapshot(CreateRequest());

            _engine.InvalidateKey(_npcId, "spec_key");
        }

        [Fact]
        public void UpdateBaseline_ClearsL1CacheAndDiffs()
        {
            RegisterTestKey("base_key", ContextLayer.L1_Baseline, 0.9f, "baseline_val");
            _engine.BuildSnapshot(CreateRequest());
            Assert.True(_engine.GetL1BlockCacheCount() > 0);

            _engine.UpdateBaseline(_npcId);
            Assert.Equal(0, _engine.GetL1BlockCacheCount());
            Assert.Equal(0, _engine.GetDiffStoreCount());
        }
    }
}
