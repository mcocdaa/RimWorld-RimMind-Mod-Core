using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Features.Context.Diff;
using Xunit;

namespace RimMind.Tests.Context.Diff
{
    public class DiffRecorderTests
    {
        private readonly DiffRecorder _recorder = new();

        [Fact]
        public void AddDiff_And_TryGetDiffStore_ReturnsAddedDiff()
        {
            var diff = new ContextDiff { Key = "Health", OldValue = "100", NewValue = "75", Layer = ContextLayer.L1_Baseline };

            _recorder.AddDiff("npc1", diff);

            Assert.True(_recorder.TryGetDiffStore("npc1", out var diffs));
            Assert.Single(diffs);
            Assert.Equal("Health", diffs[0].Key);
            Assert.Equal("100", diffs[0].OldValue);
            Assert.Equal("75", diffs[0].NewValue);
        }

        [Fact]
        public void AddDiffs_And_TryGetDiffStore_ReturnsAllDiffs()
        {
            var diffs = new[]
            {
                new ContextDiff { Key = "Health", OldValue = "100", NewValue = "75", Layer = ContextLayer.L1_Baseline },
                new ContextDiff { Key = "Mood", OldValue = "80", NewValue = "60", Layer = ContextLayer.L3_State }
            };

            _recorder.AddDiffs("npc1", diffs);

            Assert.True(_recorder.TryGetDiffStore("npc1", out var result));
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TryGetDiffStore_NonExistentNpc_ReturnsFalse()
        {
            Assert.False(_recorder.TryGetDiffStore("nonexistent", out _));
        }

        [Fact]
        public void StoreNumericValues_StoresCopy()
        {
            var values = new Dictionary<string, float> { ["Health"] = 75.5f, ["Mood"] = 80.0f };

            _recorder.StoreNumericValues("npc1", values);

            Assert.True(_recorder.KeyLastNumericValues.TryGetValue("npc1", out var stored));
            Assert.Equal(2, stored.Count);
            Assert.Equal(75.5f, stored["Health"]);
            Assert.Equal(80.0f, stored["Mood"]);
        }

        [Fact]
        public void StoreNumericValues_IsolatedCopy()
        {
            var values = new Dictionary<string, float> { ["Health"] = 75.5f };
            _recorder.StoreNumericValues("npc1", values);

            // Mutating original should not affect stored values
            values["Health"] = 0f;

            Assert.Equal(75.5f, _recorder.KeyLastNumericValues["npc1"]["Health"]);
        }

        [Fact]
        public void SetKeyLastValue_And_TryGetKeyLastValues()
        {
            _recorder.SetKeyLastValue("npc1", "Health", "100");

            Assert.True(_recorder.TryGetKeyLastValues("npc1", out var values));
            Assert.Equal("100", values["Health"]);
        }

        [Fact]
        public void SetKeyLastValue_OverwritesExisting()
        {
            _recorder.SetKeyLastValue("npc1", "Health", "100");
            _recorder.SetKeyLastValue("npc1", "Health", "75");

            Assert.True(_recorder.TryGetKeyLastValues("npc1", out var values));
            Assert.Equal("75", values["Health"]);
            Assert.Single(values);
        }

        [Fact]
        public void SetKeyLastValues_StoresCopy()
        {
            var values = new Dictionary<string, string> { ["Health"] = "100", ["Mood"] = "80" };
            _recorder.SetKeyLastValues("npc1", values);

            Assert.True(_recorder.TryGetKeyLastValues("npc1", out var stored));
            Assert.Equal(2, stored.Count);

            // Mutating original should not affect stored values
            values["Health"] = "0";
            Assert.Equal("100", stored["Health"]);
        }

        [Fact]
        public void TryGetKeyLastValues_NonExistentNpc_ReturnsFalse()
        {
            Assert.False(_recorder.TryGetKeyLastValues("nonexistent", out _));
        }

        [Fact]
        public void ClearNpcDiffs_RemovesNpcDiffs()
        {
            _recorder.AddDiff("npc1", new ContextDiff { Key = "Health", OldValue = "100", NewValue = "75", Layer = ContextLayer.L1_Baseline });

            _recorder.ClearNpcDiffs("npc1");

            Assert.False(_recorder.TryGetDiffStore("npc1", out _));
        }

        [Fact]
        public void ClearNpcDiffs_DoesNotAffectOtherNpcs()
        {
            _recorder.AddDiff("npc1", new ContextDiff { Key = "Health", OldValue = "100", NewValue = "75", Layer = ContextLayer.L1_Baseline });
            _recorder.AddDiff("npc2", new ContextDiff { Key = "Mood", OldValue = "80", NewValue = "60", Layer = ContextLayer.L3_State });

            _recorder.ClearNpcDiffs("npc1");

            Assert.False(_recorder.TryGetDiffStore("npc1", out _));
            Assert.True(_recorder.TryGetDiffStore("npc2", out _));
        }

        [Fact]
        public void RemoveNpcKeyLastValues_RemovesKeyLastValues()
        {
            _recorder.SetKeyLastValue("npc1", "Health", "100");

            _recorder.RemoveNpcKeyLastValues("npc1");

            Assert.False(_recorder.TryGetKeyLastValues("npc1", out _));
        }

        [Fact]
        public void Reset_ClearsAllStores()
        {
            _recorder.AddDiff("npc1", new ContextDiff { Key = "Health", OldValue = "100", NewValue = "75", Layer = ContextLayer.L1_Baseline });
            _recorder.SetKeyLastValue("npc1", "Health", "100");
            _recorder.StoreNumericValues("npc1", new Dictionary<string, float> { ["Health"] = 75f });

            _recorder.Reset();

            Assert.Empty(_recorder.DiffStore);
            Assert.Empty(_recorder.KeyLastValues);
            Assert.Empty(_recorder.KeyLastNumericValues);
            Assert.Equal(0, _recorder.GetDiffStoreCount());
        }

        [Fact]
        public void GetDiffStoreCount_ReturnsCorrectCount()
        {
            Assert.Equal(0, _recorder.GetDiffStoreCount());

            _recorder.AddDiff("npc1", new ContextDiff { Key = "Health", OldValue = "100", NewValue = "75", Layer = ContextLayer.L1_Baseline });

            Assert.Equal(1, _recorder.GetDiffStoreCount());

            _recorder.AddDiff("npc2", new ContextDiff { Key = "Mood", OldValue = "80", NewValue = "60", Layer = ContextLayer.L3_State });

            Assert.Equal(2, _recorder.GetDiffStoreCount());
        }

        [Fact]
        public void DiffStore_IsReadOnlyInterface()
        {
            _recorder.AddDiff("npc1", new ContextDiff { Key = "Health", OldValue = "100", NewValue = "75", Layer = ContextLayer.L1_Baseline });

            var store = _recorder.DiffStore;
            Assert.IsAssignableFrom<IReadOnlyDictionary<string, List<ContextDiff>>>(store);
            // ConcurrentDictionary implements IReadOnlyDictionary, so the interface is satisfied
            Assert.NotNull(store);
        }
    }
}
