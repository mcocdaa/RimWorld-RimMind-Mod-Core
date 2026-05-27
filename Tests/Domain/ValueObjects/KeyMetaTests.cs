using System.Collections.Generic;
using Xunit;

namespace RimMind.Tests.Domain.ValueObjects
{
    public class KeyMetaTests
    {
        private static KeyMeta CreateKeyMeta(
            string key = "testKey",
            ContextLayer layer = ContextLayer.L3_State,
            float priority = 1.0f,
            string ownerMod = "TestMod")
        {
            return new KeyMeta(key, layer, priority, _ => new List<ContextEntry>(), ownerMod);
        }

        [Fact]
        public void GetEffectivePriority_ReturnsAverageOfPriorityAndAdaptivePriority()
        {
            var meta = CreateKeyMeta(priority: 4.0f);
            meta.AdaptivePriority = 6.0f;

            var effective = meta.GetEffectivePriority();

            Assert.Equal(5.0f, effective);
        }

        [Fact]
        public void AdaptivePriority_InitiallyEqualsPriority()
        {
            var meta = CreateKeyMeta(priority: 3.5f);

            Assert.Equal(3.5f, meta.AdaptivePriority);
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var meta = new KeyMeta(
                "myKey",
                ContextLayer.L2_Environment,
                2.0f,
                _ => new List<ContextEntry>(),
                "MyMod",
                isIndexable: true);

            Assert.Equal("myKey", meta.Key);
            Assert.Equal(ContextLayer.L2_Environment, meta.Layer);
            Assert.Equal(ContextLayer.L2_Environment, meta.OriginalLayer);
            Assert.Equal(2.0f, meta.Priority);
            Assert.Equal(2.0f, meta.AdaptivePriority);
            Assert.Equal("MyMod", meta.OwnerMod);
            Assert.True(meta.IsIndexable);
        }
    }
}
