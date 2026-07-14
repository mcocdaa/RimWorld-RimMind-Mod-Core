using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
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

        private static ContextOrchestrator CreateOrchestrator(IContextKeyRegistry registry)
        {
            var services = new ContextBuildServices(
                new ContextCacheManager(embedCache: new EmbedCache()),
                new ContextDiffTracker(),
                new ContextLayerBuilder(new DefaultContextKeyProvider()),
                new BudgetScheduler());

            return new ContextOrchestrator(
                new EmptyHistoryManager(),
                npcManager: null,
                services,
                settingsProvider: null!,
                translationService: null!,
                flywheelParameterStore: null!,
                logSink: null!,
                new EmbeddingSnapshotStore(),
                registry,
                new RelevanceTableImpl());
        }

        private sealed class EmptyHistoryManager : IHistoryManager
        {
            public void AddTurn(string npcId, string userMessage, string assistantMessage, string? scenario = null) { }
            public List<(string role, string content)> GetHistory(string npcId, int maxRounds, string? scenario = null) => new();
            public int GetHistoryCount(string npcId) => 0;
            public void ClearHistory(string npcId) { }
            public void CompressIfNeeded(string npcId) { }
            public void ReplaceLastAssistantTurn(string npcId, string content) { }
            public string GetAllForSave() => string.Empty;
            public Dictionary<string, List<HistoryEntry>> GetAllForSaveDict() => new();
        }
    }
}
