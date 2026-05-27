using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Context
{
    public class ContextProviderDefTests
    {
        [Fact]
        public void ContextProviderDef_RequiredProperties_AreSet()
        {
            var def = new ContextProviderDef(
                key: "test_key",
                layer: ContextLayer.L2_Environment,
                priority: 1.0f,
                provider: (_, _) => Task.FromResult<string?>("result"));

            Assert.Equal("test_key", def.Key);
            Assert.Equal(ContextLayer.L2_Environment, def.Layer);
            Assert.Equal(1.0f, def.Priority);
            Assert.Null(def.OwnerMod);
            Assert.Equal(0, def.StalenessTicks);
            Assert.Null(def.InvalidationTriggers);
            Assert.True(def.AllowUserPin);
            Assert.False(def.IsSensitive);
        }

        [Fact]
        public void ContextProviderDef_AllProperties_SetCorrectly()
        {
            var def = new ContextProviderDef(
                key: "health",
                layer: ContextLayer.L3_State,
                priority: 2.5f,
                provider: (_, _) => Task.FromResult<string?>("healthy"),
                ownerMod: "RimMind-Advisor",
                stalenessTicks: 600,
                invalidationTriggers: new List<string> { "MoodThreshold", "NeedCritical" },
                allowUserPin: false,
                isSensitive: true);

            Assert.Equal("health", def.Key);
            Assert.Equal(ContextLayer.L3_State, def.Layer);
            Assert.Equal(2.5f, def.Priority);
            Assert.Equal("RimMind-Advisor", def.OwnerMod);
            Assert.Equal(600, def.StalenessTicks);
            Assert.NotNull(def.InvalidationTriggers);
            Assert.Equal(2, def.InvalidationTriggers.Count);
            Assert.False(def.AllowUserPin);
            Assert.True(def.IsSensitive);
        }

        [Fact]
        public async Task ContextProviderDef_Provider_ReturnsValue()
        {
            var def = new ContextProviderDef(
                key: "test",
                layer: ContextLayer.L1_Baseline,
                priority: 1.0f,
                provider: (ctx, ct) => Task.FromResult<string?>($"value_for_{ctx.NpcId}"));

            var providerCtx = new ProviderContext("dialogue", "trace-1")
            {
                NpcId = "npc-42",
                PawnId = 100
            };

            var result = await def.Provider(providerCtx, CancellationToken.None);
            Assert.Equal("value_for_npc-42", result);
        }

        [Fact]
        public async Task ContextProviderDef_Provider_CanReturnNull()
        {
            var def = new ContextProviderDef(
                key: "empty",
                layer: ContextLayer.L2_Environment,
                priority: 0.5f,
                provider: (_, _) => Task.FromResult<string?>(null));

            var providerCtx = new ProviderContext("dialogue", "trace-2");

            var result = await def.Provider(providerCtx, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public void ProviderContext_RecordEquality()
        {
            var ctx1 = new ProviderContext("dialogue", "t1")
            {
                NpcId = "npc-1",
                PawnId = 10,
                MapId = 5
            };

            var ctx2 = new ProviderContext("dialogue", "t1")
            {
                NpcId = "npc-1",
                PawnId = 10,
                MapId = 5
            };

            Assert.Equal(ctx1, ctx2);
            Assert.Equal(ctx1.GetHashCode(), ctx2.GetHashCode());
        }

        [Fact]
        public void ProviderContext_DifferentValues_NotEqual()
        {
            var ctx1 = new ProviderContext("dialogue", "t1");
            var ctx2 = new ProviderContext("monologue", "t1");

            Assert.NotEqual(ctx1, ctx2);
        }

        [Fact]
        public void ProviderContext_Hints_CarryData()
        {
            var hints = new Dictionary<string, object?>
            {
                { "key1", "value1" },
                { "key2", 42 }
            };

            var ctx = new ProviderContext("dialogue", "t1")
            {
                Hints = hints
            };

            Assert.NotNull(ctx.Hints);
            Assert.Equal(2, ctx.Hints.Count);
            Assert.Equal("value1", ctx.Hints["key1"]);
            Assert.Equal(42, ctx.Hints["key2"]);
        }

        [Fact]
        public void ContextProviderDef_NullKey_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ContextProviderDef(
                key: null!,
                layer: ContextLayer.L2_Environment,
                priority: 1.0f,
                provider: (_, _) => Task.FromResult<string?>("v")));
        }

        [Fact]
        public void ContextProviderDef_NullProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ContextProviderDef(
                key: "test",
                layer: ContextLayer.L2_Environment,
                priority: 1.0f,
                provider: null!));
        }
    }
}
