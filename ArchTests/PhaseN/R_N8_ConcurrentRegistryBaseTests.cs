using FluentAssertions;
using RimMind.Application.Common.Behaviours;
using System;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN
{
    public class R_N8_ConcurrentRegistryBaseTests
    {
        private sealed class Item
        {
            public string Id { get; set; } = "";
            public string OwnerModId { get; set; } = "";
        }

        private sealed class TestRegistry : ConcurrentRegistryBase<string, Item>
        {
            public TestRegistry() : base(i => i.Id, i => i.OwnerModId) { }
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Register_And_FindById_ShouldWork()
        {
            var reg = new TestRegistry();
            var item = new Item { Id = "a", OwnerModId = "core" };
            reg.Register(item);
            reg.FindById("a").Should().BeSameAs(item);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Register_Null_ShouldBe_NoOp()
        {
            var reg = new TestRegistry();
            reg.Register(null!);
            reg.All.Should().BeEmpty();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Register_DuplicateKey_ShouldOverwrite()
        {
            var reg = new TestRegistry();
            var first = new Item { Id = "a", OwnerModId = "core" };
            var second = new Item { Id = "a", OwnerModId = "core" };
            reg.Register(first);
            reg.Register(second);
            reg.FindById("a").Should().BeSameAs(second);
            reg.All.Should().HaveCount(1);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Unregister_ShouldRemove_And_ReturnTrue()
        {
            var reg = new TestRegistry();
            reg.Register(new Item { Id = "a", OwnerModId = "core" });
            reg.Unregister("a").Should().BeTrue();
            reg.FindById("a").Should().BeNull();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Unregister_NonExistent_ShouldReturn_False()
        {
            var reg = new TestRegistry();
            reg.Unregister("nope").Should().BeFalse();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void UnregisterByOwner_ShouldRemove_OnlyMatchingItems()
        {
            var reg = new TestRegistry();
            reg.Register(new Item { Id = "a", OwnerModId = "core" });
            reg.Register(new Item { Id = "b", OwnerModId = "other" });

            var removed = reg.UnregisterByOwner("core");
            removed.Should().Be(1);
            reg.FindById("a").Should().BeNull();
            reg.FindById("b").Should().NotBeNull();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void UnregisterByOwner_Null_ShouldThrow()
        {
            var reg = new TestRegistry();
            Action act = () => reg.UnregisterByOwner(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void All_ShouldReturn_Snapshot()
        {
            var reg = new TestRegistry();
            reg.Register(new Item { Id = "a", OwnerModId = "core" });
            reg.Register(new Item { Id = "b", OwnerModId = "core" });
            var snapshot = reg.All;
            snapshot.Should().HaveCount(2);
            reg.Register(new Item { Id = "c", OwnerModId = "core" });
            snapshot.Should().HaveCount(2, "snapshot should not reflect later mutations");
        }
    }
}
