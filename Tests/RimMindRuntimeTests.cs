using System;
using RimMind.Kernel.Bus;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Core.Runtime;
using RimMind.Kernel.Flywheel;
using Verse;
using Xunit;

namespace RimMind.Tests.Runtime
{
    public class RimMindRuntimeResetTests : IDisposable
    {
        public RimMindRuntimeResetTests()
        {
            RimMindRuntime.ResetInstance();
            RimMindRuntime.Initialize();
        }

        public void Dispose()
        {
            RimMindRuntime.ResetInstance();
        }

        [Fact]
        public void Reset_EventBus_IsNotNull()
        {
            var runtime = RimMindRuntime.Instance;
            runtime.Reset();

            Assert.NotNull(runtime.EventBus);
        }

        [Fact]
        public void Reset_ContextEngine_IsNotNull()
        {
            var runtime = RimMindRuntime.Instance;
            runtime.Reset();

            Assert.NotNull(runtime.ContextEngine);
        }

        [Fact]
        public void Reset_Telemetry_IsNotNull()
        {
            var runtime = RimMindRuntime.Instance;
            runtime.Reset();

            Assert.NotNull(runtime.Telemetry);
        }

        [Fact]
        public void Reset_IsShutdown_IsFalse()
        {
            var runtime = RimMindRuntime.Instance;
            runtime.Reset();

            Assert.False(runtime.IsShutdown);
        }

        [Fact]
        public void Reset_ParameterTunersList_IsEmpty()
        {
            var runtime = RimMindRuntime.Instance;
            runtime.Reset();

            Assert.Empty(runtime.ParameterTunersList);
        }

        [Fact]
        public void Reset_SensorProvidersList_IsEmpty()
        {
            var runtime = RimMindRuntime.Instance;
            runtime.Reset();

            Assert.Empty(runtime.SensorProvidersList);
        }

        [Fact]
        public void Reset_CalledTwice_DoesNotThrow()
        {
            var runtime = RimMindRuntime.Instance;
            runtime.Reset();

            var ex = Record.Exception(() => runtime.Reset());
            Assert.Null(ex);
        }

        [Fact]
        public void Reset_EventBus_IsNewInstance()
        {
            var runtime = RimMindRuntime.Instance;
            var originalBus = runtime.EventBus;
            runtime.Reset();

            Assert.NotSame(originalBus, runtime.EventBus);
        }

        [Fact]
        public void Reset_ContextEngine_IsNewInstance()
        {
            var runtime = RimMindRuntime.Instance;
            var originalEngine = runtime.ContextEngine;
            runtime.Reset();

            Assert.NotSame(originalEngine, runtime.ContextEngine);
        }
    }

    public class RimMindRuntimeWithOverridesTests : IDisposable
    {
        public RimMindRuntimeWithOverridesTests()
        {
            RimMindRuntime.ResetInstance();
            RimMindRuntime.Initialize();
        }

        public void Dispose()
        {
            RimMindRuntime.ResetInstance();
        }

        [Fact]
        public void WithOverrides_EventBus_ReturnsFakeInScope()
        {
            var runtime = RimMindRuntime.Instance;
            var originalBus = runtime.EventBus;
            var fakeBus = new EventBusAdapter(new AgentBusImpl());

            using (runtime.WithOverrides(o => o.EventBus = fakeBus))
            {
                Assert.Same(fakeBus, runtime.EventBus);
            }

            Assert.Same(originalBus, runtime.EventBus);
        }

        [Fact]
        public void WithOverrides_ContextEngine_ReturnsFakeInScope()
        {
            var runtime = RimMindRuntime.Instance;
            var originalEngine = runtime.ContextEngine;
            var fakeEngine = new ContextEngine(new HistoryManager());

            using (runtime.WithOverrides(o => o.ContextEngine = fakeEngine))
            {
                Assert.Same(fakeEngine, runtime.ContextEngine);
            }

            Assert.Same(originalEngine, runtime.ContextEngine);
        }

        [Fact]
        public void WithOverrides_NestedScope_RestoresCorrectly()
        {
            var runtime = RimMindRuntime.Instance;
            var originalBus = runtime.EventBus;
            var fakeBus1 = new EventBusAdapter(new AgentBusImpl());
            var fakeBus2 = new EventBusAdapter(new AgentBusImpl());

            using (runtime.WithOverrides(o => o.EventBus = fakeBus1))
            {
                Assert.Same(fakeBus1, runtime.EventBus);

                using (runtime.WithOverrides(o => o.EventBus = fakeBus2))
                {
                    Assert.Same(fakeBus2, runtime.EventBus);
                }

                Assert.Same(fakeBus1, runtime.EventBus);
            }

            Assert.Same(originalBus, runtime.EventBus);
        }

        [Fact]
        public void WithOverrides_DisposeCalledTwice_DoesNotCorruptState()
        {
            var runtime = RimMindRuntime.Instance;
            var originalBus = runtime.EventBus;
            var fakeBus = new EventBusAdapter(new AgentBusImpl());

            var scope = runtime.WithOverrides(o => o.EventBus = fakeBus);
            Assert.Same(fakeBus, runtime.EventBus);

            scope.Dispose();
            Assert.Same(originalBus, runtime.EventBus);

            scope.Dispose();
            Assert.Same(originalBus, runtime.EventBus);
        }

        [Fact]
        public void WithOverrides_NullOverride_DoesNotChangeProperty()
        {
            var runtime = RimMindRuntime.Instance;
            var originalBus = runtime.EventBus;

            using (runtime.WithOverrides(o => { }))
            {
                Assert.Same(originalBus, runtime.EventBus);
            }

            Assert.Same(originalBus, runtime.EventBus);
        }
    }
}
