using System;
using System.Collections.Generic;
using System.Threading;
using RimMind.Core.Flywheel;
using Xunit;

namespace RimMind.Core.Tests
{
    public class FlywheelParameterStoreTests
    {
        private FlywheelParameterStore CreateStore()
        {
            return new FlywheelParameterStore();
        }

        [Fact]
        public void Get_DefaultValue_ReturnsDefault()
        {
            var store = CreateStore();
            Assert.Equal(0.4f, store.Get("w1"), 4);
            Assert.Equal(0.6f, store.Get("w2"), 4);
            Assert.Equal(0.01f, store.Get("Alpha"), 4);
            Assert.Equal(4000f, store.Get("TotalBudget"), 4);
        }

        [Fact]
        public void Get_UnknownKey_Returns0()
        {
            var store = CreateStore();
            Assert.Equal(0f, store.Get("nonexistent"), 4);
        }

        [Fact]
        public void UpdateParameter_ChangesValue()
        {
            var store = CreateStore();
            store.UpdateParameter("w1", 0.8f);
            Assert.Equal(0.8f, store.Get("w1"), 4);
        }

        [Fact]
        public void UpdateParameter_FiresOnParameterChanged()
        {
            var store = CreateStore();
            string? changedKey = null;
            float changedValue = 0f;
            store.OnParameterChanged += (key, value) =>
            {
                changedKey = key;
                changedValue = value;
            };

            store.UpdateParameter("w1", 0.9f);
            Assert.Equal("w1", changedKey);
            Assert.Equal(0.9f, changedValue, 4);
        }

        [Fact]
        public void UpdateParameter_NoEvent_WhenDeltaBelowThreshold()
        {
            var store = CreateStore();
            bool fired = false;
            store.OnParameterChanged += (key, value) => fired = true;

            store.UpdateParameter("w1", store.Get("w1") + 0.00001f);
            Assert.False(fired);
        }

        [Fact]
        public void ResetToDefault_RestoresValue()
        {
            var store = CreateStore();
            store.UpdateParameter("w1", 0.99f);
            store.ResetToDefault("w1");
            Assert.Equal(0.4f, store.Get("w1"), 4);
        }

        [Fact]
        public void ResetAll_RestoresAllValues()
        {
            var store = CreateStore();
            store.UpdateParameter("w1", 0.99f);
            store.UpdateParameter("Alpha", 0.5f);
            store.ResetAll();
            Assert.Equal(0.4f, store.Get("w1"), 4);
            Assert.Equal(0.01f, store.Get("Alpha"), 4);
        }

        [Fact]
        public void GetAll_ReturnsSnapshot()
        {
            var store = CreateStore();
            var all = store.GetAll();
            Assert.True(all.ContainsKey("w1"));
            Assert.True(all.ContainsKey("TotalBudget"));
        }

        [Fact]
        public void GetDefaults_ReturnsSnapshot()
        {
            var store = CreateStore();
            var defaults = store.GetDefaults();
            Assert.Equal(0.4f, defaults["w1"], 4);
            Assert.Equal(4000f, defaults["TotalBudget"], 4);
        }

        [Fact]
        public void TotalBudget_ReturnsInt()
        {
            var store = CreateStore();
            Assert.Equal(4000, store.TotalBudget);
        }

        [Fact]
        public void UpdateParameter_NewKey_CreatesEntry()
        {
            var store = CreateStore();
            store.UpdateParameter("custom_key", 42f);
            Assert.Equal(42f, store.Get("custom_key"), 4);
        }

        [Fact]
        public void UpdateParameter_ClampsAlpha_ToRange()
        {
            var store = CreateStore();
            store.UpdateParameter("Alpha", -0.5f);
            Assert.Equal(0.0f, store.Get("Alpha"), 4);

            store.UpdateParameter("Alpha", 2.0f);
            Assert.Equal(1.0f, store.Get("Alpha"), 4);
        }

        [Fact]
        public void UpdateParameter_ClampsPromoteThreshold_ToRange()
        {
            var store = CreateStore();
            store.UpdateParameter("PromoteThreshold", -0.1f);
            Assert.Equal(0.0f, store.Get("PromoteThreshold"), 4);

            store.UpdateParameter("PromoteThreshold", 1.5f);
            Assert.Equal(1.0f, store.Get("PromoteThreshold"), 4);
        }

        [Fact]
        public void UpdateParameter_ClampsTotalBudget_ToRange()
        {
            var store = CreateStore();
            store.UpdateParameter("TotalBudget", 50f);
            Assert.Equal(100f, store.Get("TotalBudget"), 4);

            store.UpdateParameter("TotalBudget", 50000f);
            Assert.Equal(32000f, store.Get("TotalBudget"), 4);
        }

        [Fact]
        public void UpdateParameter_ClampsDecayRate_ToRange()
        {
            var store = CreateStore();
            store.UpdateParameter("DecayRate", -0.5f);
            Assert.Equal(0.0f, store.Get("DecayRate"), 4);

            store.UpdateParameter("DecayRate", 2.0f);
            Assert.Equal(1.0f, store.Get("DecayRate"), 4);
        }

        [Fact]
        public void UpdateParameter_DoesNotClamp_UnknownKey()
        {
            var store = CreateStore();
            store.UpdateParameter("custom_key", -999f);
            Assert.Equal(-999f, store.Get("custom_key"), 4);
        }

        [Fact]
        public void UpdateParameter_ClampedValue_StillFiresEvent()
        {
            var store = CreateStore();
            bool fired = false;
            store.OnParameterChanged += (key, value) => fired = true;
            store.UpdateParameter("Alpha", 5.0f);
            Assert.True(fired);
            Assert.Equal(1.0f, store.Get("Alpha"), 4);
        }
    }
}
