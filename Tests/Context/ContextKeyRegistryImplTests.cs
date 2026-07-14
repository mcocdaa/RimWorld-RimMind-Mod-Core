using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Features.AgentBus;
using System.Collections.Generic;
using RimMind.Application.Features.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Domain.Events;
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

        [Fact]
        public async Task Register_ProviderWithInvalidationTrigger_RecomputesAfterBusPublish()
        {
            var ticks = new RegistryTickProvider { TicksGame = 100 };
            var bus = new AgentBusImpl();
            bus.RegisterEventType("ProviderChanged", typeof(AgentBusEvent));
            var cache = new ProviderCache(bus, tickProvider: ticks);
            var registry = new ContextKeyRegistryImpl(providerCache: cache);
            int calls = 0;
            var def = new ContextProviderDef(
                key: "invalidated_provider",
                layer: ContextLayer.L2_Environment,
                priority: 1.0f,
                provider: (_, _) => Task.FromResult<string?>("value_" + ++calls),
                stalenessTicks: 600,
                invalidationTriggers: new[] { "ProviderChanged" });
            var context = new ProviderContext("dialogue", "trace");

            registry.Register(def);
            var first = await cache.GetOrComputeAsync(def, context, CancellationToken.None);
            bus.Publish(new AgentBusEvent());
            var second = await cache.GetOrComputeAsync(def, context, CancellationToken.None);

            Assert.Equal("value_1", first);
            Assert.Equal("value_2", second);
            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task Register_SameProviderInvalidationTriggerTwice_SubscribesOnlyOnce()
        {
            var ticks = new RegistryTickProvider { TicksGame = 100 };
            var bus = new CountingAgentBus();
            var cache = new ProviderCache(bus, tickProvider: ticks);
            var registry = new ContextKeyRegistryImpl(providerCache: cache);
            var def = new ContextProviderDef(
                key: "deduplicated_provider",
                layer: ContextLayer.L2_Environment,
                priority: 1.0f,
                provider: (_, _) => Task.FromResult<string?>("value"),
                invalidationTriggers: new[] { "ProviderChanged" });

            registry.Register(def);
            registry.Register(def);

            Assert.Equal(1, bus.SubscribeByNameCalls);
            await Task.CompletedTask;
        }

        private sealed class RegistryTickProvider : ITickProvider
        {
            public int TicksGame { get; set; }
        }

        private sealed class CountingAgentBus : IAgentBus
        {
            public int SubscribeByNameCalls { get; private set; }

            public event System.Action? SubscribersCleared;

            public void Publish<T>(T evt) where T : AgentBusEvent { }
            public void PublishFromBackground<T>(T evt) where T : AgentBusEvent { }
            public string Subscribe<T>(System.Action<T> handler) where T : AgentBusEvent => "unused";
            public void Subscribe<T>(string key, System.Action<T> handler) where T : AgentBusEvent { }
            public void Unsubscribe<T>(string key) where T : AgentBusEvent { }
            [System.Obsolete]
            public void Unsubscribe<T>(System.Action<T> handler) where T : AgentBusEvent { }
            public string SubscribeByName(string eventTypeName, System.Action<AgentBusEvent> handler)
            {
                SubscribeByNameCalls++;
                return "subscription_" + SubscribeByNameCalls;
            }
            public void SetPipeline(RimMind.Application.Common.Interfaces.Pipeline.IPipeline<RimMind.Application.Common.Models.Pipeline.BusPublishContext> pipeline) { }
            public void FlushBackgroundQueue() { }
            public void ClearAllSubscribers() => SubscribersCleared?.Invoke();
            public int GetHandlerCount() => 0;
            public int GetBackgroundQueueCount() => 0;
            public System.Action<AgentBusEvent>? DispatchAction => null;
            public void RegisterEventType(string name, System.Type eventType) { }
        }
    }
}
