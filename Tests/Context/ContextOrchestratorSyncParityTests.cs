using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Cache;
using RimMind.Presentation.Context;
using Xunit;

namespace RimMind.Tests.Context
{
    public class ContextOrchestratorAsyncProviderTests
    {
        [Fact]
        public async Task BuildSnapshotFromEnvelopeAsync_AsyncProviderContent_IsIncluded()
        {
            var registry = new ContextKeyRegistryImpl();
            registry.Register(new ContextProviderDef(
                key: "async_provider",
                layer: ContextLayer.L2_Environment,
                priority: 1f,
                provider: async (_, _) =>
                {
                    await Task.Yield();
                    return "async provider content";
                }));

            var orchestrator = CreateOrchestrator(registry);

            var snapshot = await orchestrator.BuildSnapshotFromEnvelopeAsync("npc-1", "hello");

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot!.Messages, message =>
                message.Content.Contains("async provider content"));
        }

        [Fact]
        public async Task BuildSnapshotFromEnvelopeAsync_PreservesHealthyLayerAndLogs_WhenAnotherLayerFaults()
        {
            const string secretProviderContent = "provider failure detail must not be logged";
            var registry = new ContextKeyRegistryImpl();
            registry.Register(new ContextProviderDef(
                key: "faulted_l2_provider",
                layer: ContextLayer.L2_Environment,
                priority: 1f,
                provider: (_, _) => throw new InvalidOperationException(secretProviderContent)));
            registry.Register(new ContextProviderDef(
                key: "healthy_l3_provider",
                layer: ContextLayer.L3_State,
                priority: 1f,
                provider: (_, _) => Task.FromResult<string?>("healthy L3 context")));

            var logSink = new CapturingLogSink();
            var orchestrator = CreateOrchestrator(registry, logSink);

            var snapshot = await orchestrator.BuildSnapshotFromEnvelopeAsync("npc-fault-test", "hello", scenarioId: "scenario-fault-test");

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot!.Messages, message => message.Content.Contains("healthy L3 context"));
            string diagnostic = Assert.Single(logSink.BackgroundWarnings);
            Assert.Contains("layer=L2", diagnostic);
            Assert.Contains("npc=npc-fault-test", diagnostic);
            Assert.Contains("scenario=scenario-fault-test", diagnostic);
            Assert.DoesNotContain(secretProviderContent, diagnostic);
        }

        [Fact]
        public async Task BuildSnapshotFromEnvelopeAsync_PreservesHealthyLayerAndLogs_WhenCachedProviderFaults()
        {
            const string secretProviderContent = "cached provider failure detail must not be logged";
            int faultedProviderCalls = 0;
            int healthyProviderCalls = 0;
            var registry = new ContextKeyRegistryImpl();
            registry.Register(new ContextProviderDef(
                key: "faulted_cached_l2_provider",
                layer: ContextLayer.L2_Environment,
                priority: 1f,
                provider: (_, _) =>
                {
                    faultedProviderCalls++;
                    throw new InvalidOperationException(secretProviderContent);
                },
                stalenessTicks: 600));
            registry.Register(new ContextProviderDef(
                key: "healthy_cached_l3_provider",
                layer: ContextLayer.L3_State,
                priority: 1f,
                provider: (_, _) =>
                {
                    healthyProviderCalls++;
                    return Task.FromResult<string?>("healthy cached L3 context");
                },
                stalenessTicks: 600));

            var logSink = new CapturingLogSink();
            var cache = new ProviderCache(log: logSink);
            var orchestrator = CreateOrchestrator(registry, logSink, cache);

            var snapshot = await orchestrator.BuildSnapshotFromEnvelopeAsync("npc-cache-fault-test", "hello", scenarioId: "scenario-cache-fault-test");
            var secondSnapshot = await orchestrator.BuildSnapshotFromEnvelopeAsync("npc-cache-fault-test", "hello", scenarioId: "scenario-cache-fault-test");

            Assert.NotNull(snapshot);
            Assert.NotNull(secondSnapshot);
            Assert.Contains(snapshot!.Messages, message => message.Content.Contains("healthy cached L3 context"));
            Assert.Contains(secondSnapshot!.Messages, message => message.Content.Contains("healthy cached L3 context"));
            Assert.Equal(1, healthyProviderCalls);
            Assert.Equal(1, cache.Count);
            Assert.Equal(2, faultedProviderCalls);
            Assert.Equal(2, logSink.BackgroundWarnings.Count);
            string diagnostic = logSink.BackgroundWarnings[0];
            Assert.Contains("layer=L0", diagnostic);
            Assert.Contains("npc=npc-cache-fault-test", diagnostic);
            Assert.Contains("scenario=scenario-cache-fault-test", diagnostic);
            Assert.DoesNotContain(secretProviderContent, diagnostic);
            Assert.All(logSink.BackgroundWarnings, warning => Assert.DoesNotContain(secretProviderContent, warning));
            Assert.DoesNotContain(logSink.WarningMessages, message => message.Contains(secretProviderContent));
        }

        private static ContextOrchestrator CreateOrchestrator(
            IContextKeyRegistry registry,
            ILogSink? logSink = null,
            ProviderCache? providerCache = null)
        {
            var services = new ContextBuildServices(
                new ContextCacheManager(embedCache: new EmbedCache()),
                new ContextDiffTracker(),
                new ContextLayerBuilder(new DefaultContextKeyProvider()),
                new BudgetScheduler());

            return new ContextOrchestrator(
                new EmptyHistoryManager(),
                npcManagers: new EmptyNpcManagerAccessor(),
                services,
                settingsProvider: null!,
                translationService: null!,
                flywheelParameterStore: null!,
                logSink: logSink ?? new CapturingLogSink(),
                new EmbeddingSnapshotStore(),
                registry,
                new RelevanceTableImpl(),
                providerCache);
        }

        private sealed class EmptyNpcManagerAccessor : INpcManagerAccessor
        {
            public INpcManager? Current => null;
        }

        private sealed class EmptyHistoryManager : IHistoryManager
        {
            public void AddTurn(string npcId, string userMessage, string assistantMessage, string? scenario = null) { }
            public void AddPendingTurn(string npcId, string turnId, string userMessage, string assistantPlaceholder, string? scenario = null) { }
            public List<(string role, string content)> GetHistory(string npcId, int maxRounds, string? scenario = null) => new();
            public List<(string role, string content)> GetHistoryForDisplay(string npcId, int maxRounds, string? scenario = null) => new();
            public int GetHistoryCount(string npcId) => 0;
            public void ClearHistory(string npcId) { }
            public void CompressIfNeeded(string npcId) { }
            public void ReplaceLastAssistantTurn(string npcId, string content) { }
            public bool ReplaceAssistantTurn(string npcId, string turnId, string content) => false;
            public bool RemoveTurn(string npcId, string turnId) => false;
            public string GetAllForSave() => string.Empty;
            public Dictionary<string, List<HistoryEntry>> GetAllForSaveDict() => new();
        }

        private sealed class CapturingLogSink : ILogSink
        {
            public List<string> BackgroundWarnings { get; } = new();
            public List<string> WarningMessages { get; } = new();

            public void Message(string msg) { }
            public void Warning(string msg) => WarningMessages.Add(msg);
            public void Error(string msg) { }
            public void LogFromBackground(string msg, bool isWarning = false)
            {
                if (isWarning)
                    BackgroundWarnings.Add(msg);
            }
        }
    }
}
