using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Agent.InnerVoice;
using RimMind.Application.Features.Context;
using RimMind.Domain.Interfaces;
using RimMind.Presentation.Context;

namespace RimMind.Presentation.Runtime.Composition
{
    internal sealed class ContextCompositionServices
    {
        public IProviderRegistry ProviderRegistry { get; init; } = null!;
        public IHistoryManager HistoryManager { get; init; } = null!;
        public IContextEngine ContextEngine { get; init; } = null!;
        public IContextKeyRegistry ContextKeyRegistry { get; init; } = null!;
        public IContextKeyProvider ContextKeyProvider { get; init; } = null!;
        public IContextCacheManager CacheManager { get; init; } = null!;
        public IContextDiffTracker DiffTracker { get; init; } = null!;
        public IContextLayerBuilder LayerBuilder { get; init; } = null!;
        public IBudgetScheduler BudgetScheduler { get; init; } = null!;
        public IRelevanceTable RelevanceTable { get; init; } = null!;
        public IRelevanceLearner RelevanceLearner { get; init; } = null!;
        public InnerVoiceHandler InnerVoiceHandler { get; init; } = null!;
    }

    internal static class ContextComposition
    {
        public static ContextCompositionServices Register(
            ISettingsProvider resolvedSettings,
            IAgentBus agentBus,
            ITickProvider tickProvider,
            ILogSink logSink,
            INpcManager? npcManager,
            ITranslationService translationService,
            IFlywheelParameterStore flywheelParameterStore,
            IEmbedCache embedCache)
        {
            var providerRegistry = new ProviderRegistry();
            RimMindServiceLocator.Register<IProviderRegistry>(providerRegistry);

            var historyManager = new HistoryManager(tickProvider);
            RimMindServiceLocator.Register<IHistoryManager>(historyManager);

            var innerVoiceHandler = new InnerVoiceHandler(agentBus, tickProvider, logSink);
            innerVoiceHandler.StartListening();
            RimMindServiceLocator.Register(innerVoiceHandler);

            var cacheManager = new ContextCacheManager(logSink, embedCache);
            var diffTracker = new ContextDiffTracker(logSink);
            var keyProvider = new DefaultContextKeyProvider();
            var layerBuilder = new ContextLayerBuilder(keyProvider, logSink);
            var providerCache = new ProviderCache(agentBus, logSink, tickProvider);
            var keyRegistryImpl = new ContextKeyRegistryImpl(logSink, providerCache);
            var relevanceTableImpl = new RelevanceTableImpl();
            var relevanceLearner = new RelevanceLearner(tickProvider);
            var budgetScheduler = new BudgetScheduler(relevanceTableImpl, relevanceLearner, tickProvider, cacheManager.EmbedCache);
            var schemaRegistry = new SchemaRegistry(logSink);
            var buildServices = new ContextBuildServices(cacheManager, diffTracker, layerBuilder, budgetScheduler);

            RimMindServiceLocator.Register<IBudgetScheduler>(budgetScheduler);
            RimMindServiceLocator.Register<IContextCacheManager>(cacheManager);
            RimMindServiceLocator.Register<IContextDiffTracker>(diffTracker);
            RimMindServiceLocator.Register<IContextLayerBuilder>(layerBuilder);
            RimMindServiceLocator.Register<IContextKeyRegistry>(keyRegistryImpl);
            RimMindServiceLocator.Register<IRelevanceTable>(relevanceTableImpl);
            RimMindServiceLocator.Register<IRelevanceLearner>(relevanceLearner);
            RimMindServiceLocator.Register(schemaRegistry);

            var embeddingSnapshotStore = new EmbeddingSnapshotStore();
            var contextEngine = new ContextOrchestrator(
                historyManager,
                npcManager,
                buildServices,
                resolvedSettings,
                translationService,
                flywheelParameterStore,
                logSink,
                embeddingSnapshotStore,
                keyRegistryImpl,
                relevanceTableImpl,
                providerCache,
                tickProvider);

            RimMindServiceLocator.Register<IContextEngine>(contextEngine);
            RimMindServiceLocator.Register<IContextKeyProvider>(keyProvider);
            RimMindServiceLocator.Register<IContextBuilder>(contextEngine);
            RimMindServiceLocator.Register<IContextCache>(contextEngine);

            return new ContextCompositionServices
            {
                ProviderRegistry = providerRegistry,
                HistoryManager = historyManager,
                ContextEngine = contextEngine,
                ContextKeyRegistry = keyRegistryImpl,
                ContextKeyProvider = keyProvider,
                CacheManager = cacheManager,
                DiffTracker = diffTracker,
                LayerBuilder = layerBuilder,
                BudgetScheduler = budgetScheduler,
                RelevanceTable = relevanceTableImpl,
                RelevanceLearner = relevanceLearner,
                InnerVoiceHandler = innerVoiceHandler
            };
        }
    }
}
