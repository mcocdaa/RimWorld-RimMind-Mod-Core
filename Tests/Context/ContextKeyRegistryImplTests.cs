using System.Collections.Generic;
using RimMind.Application.Features.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    public class ContextKeyRegistryImplTests
    {
        private readonly ContextKeyRegistryImpl _registry;

        public ContextKeyRegistryImplTests()
        {
            _registry = new ContextKeyRegistryImpl(logSink: null);
        }

        [Fact]
        public void Register_AndGetAll_ReturnsRegisteredKey()
        {
            var meta = new KeyMeta("test_key", ContextLayer.L0_Static, 1.0f,
                _ => new List<ContextEntry>(), "TestMod");

            _registry.Register(meta);

            var all = _registry.GetAll();
            Assert.Single(all);
            Assert.Equal("test_key", all[0].Key);
            Assert.Equal(ContextLayer.L0_Static, all[0].Layer);
            Assert.Equal("TestMod", all[0].OwnerMod);
        }

        [Fact]
        public void Register_DuplicateKey_Overwrites()
        {
            var meta1 = new KeyMeta("dup_key", ContextLayer.L0_Static, 1.0f,
                _ => new List<ContextEntry>(), "Mod1");
            var meta2 = new KeyMeta("dup_key", ContextLayer.L1_Baseline, 0.9f,
                _ => new List<ContextEntry>(), "Mod2");

            _registry.Register(meta1);
            _registry.Register(meta2);

            var all = _registry.GetAll();
            Assert.Single(all);
            Assert.Equal("Mod2", all[0].OwnerMod);
            Assert.Equal(ContextLayer.L1_Baseline, all[0].Layer);
        }

        [Fact]
        public void Unregister_ExistingKey_ReturnsTrue()
        {
            var meta = new KeyMeta("rem_key", ContextLayer.L0_Static, 1.0f,
                _ => new List<ContextEntry>(), "TestMod");
            _registry.Register(meta);

            bool result = _registry.Unregister("rem_key");

            Assert.True(result);
            Assert.Empty(_registry.GetAll());
        }

        [Fact]
        public void Unregister_NonExistingKey_ReturnsFalse()
        {
            bool result = _registry.Unregister("nonexistent");

            Assert.False(result);
        }

        [Fact]
        public void Get_ExistingKey_ReturnsKeyMeta()
        {
            var meta = new KeyMeta("get_key", ContextLayer.L2_Environment, 0.7f,
                _ => new List<ContextEntry>(), "TestMod");
            _registry.Register(meta);

            var result = _registry.Get("get_key");

            Assert.NotNull(result);
            Assert.Equal("get_key", result.Key);
            Assert.Equal(0.7f, result.Priority);
        }

        [Fact]
        public void Get_NonExistingKey_ReturnsNull()
        {
            var result = _registry.Get("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public void GetAll_Empty_ReturnsEmptyList()
        {
            var all = _registry.GetAll();

            Assert.Empty(all);
        }

        [Fact]
        public void GetAll_ReturnsNewList_EachCall()
        {
            var meta = new KeyMeta("isolation_key", ContextLayer.L0_Static, 1.0f,
                _ => new List<ContextEntry>(), "TestMod");
            _registry.Register(meta);

            var list1 = _registry.GetAll();
            var list2 = _registry.GetAll();

            Assert.NotSame(list1, list2);
            Assert.Equal(list1.Count, list2.Count);
        }

        [Fact]
        public void Clear_RemovesAllKeys()
        {
            _registry.Register(new KeyMeta("key1", ContextLayer.L0_Static, 1.0f,
                _ => new List<ContextEntry>(), "TestMod"));
            _registry.Register(new KeyMeta("key2", ContextLayer.L1_Baseline, 0.9f,
                _ => new List<ContextEntry>(), "TestMod"));

            _registry.Clear();

            Assert.Empty(_registry.GetAll());
            Assert.Null(_registry.Get("key1"));
            Assert.Null(_registry.Get("key2"));
        }

        [Fact]
        public void Register_MultipleKeys_AllRegistered()
        {
            _registry.Register(new KeyMeta("key_a", ContextLayer.L0_Static, 1.0f,
                _ => new List<ContextEntry>(), "ModA"));
            _registry.Register(new KeyMeta("key_b", ContextLayer.L1_Baseline, 0.9f,
                _ => new List<ContextEntry>(), "ModB"));
            _registry.Register(new KeyMeta("key_c", ContextLayer.L2_Environment, 0.7f,
                _ => new List<ContextEntry>(), "ModC"));

            var all = _registry.GetAll();
            Assert.Equal(3, all.Count);
        }
    }
}
