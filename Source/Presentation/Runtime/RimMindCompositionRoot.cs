using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Defaults;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Features.Agent.InnerVoice;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Pipeline.Bus;
using RimMind.Application.Features.Registry;
using RimMind.Infrastructure;
using RimMind.Infrastructure.Persistence;
using RimMind.Infrastructure.UI;
using RimMind.Application.Common.Models.Agent;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Context;
using RimMind.Presentation.Llm;
using RimMind.Presentation.Settings;
using RimMind.Application.Features.Pipeline.AI;
using RimMind.Application.Features.Pipeline.Context;
using RimMind.Application.Features.Pipeline.Npc;
using RimMind.Presentation.Pipeline.AI;
using RimMind.Presentation.Pipeline.Context;
using RimMind.Presentation.Pipeline.Npc;
using RimMind.Presentation.Sensor;

namespace RimMind.Presentation.Runtime
{
    /// <summary>
    /// Composition Root: responsible for all service instantiation and DI registration.
    /// Extracted from RimMindRuntime to satisfy SRP.
    /// </summary>
    internal sealed class RimMindCompositionRoot
    {
        public sealed class CompositionResult
        {
            // Application layer services
            public IAgentBus AgentBus { get; init; } = null!;
            public IToolRegistry ToolRegistry { get; init; } = null!;
            public IAIRequestQueue Queue { get; init; } = null!;
            public ITelemetryCollector Telemetry { get; init; } = null!;
            public IFlywheelParameterStore ParameterStore { get; init; } = null!;

            // Infrastructure layer services
            public IAudioPlayer AudioPlayer { get; init; } = null!;
            public ITickProvider TickProvider { get; init; } = null!;
            public IThreadChecker ThreadChecker { get; init; } = null!;
            public ILogSink LogSink { get; init; } = null!;
            public ITranslationService TranslationService { get; init; } = null!;
            public IGameMechanismRegistry MechanismRegistry { get; init; } = null!;
            public IWindowService? WindowService { get; init; }
            public IPlayer2Lifecycle Player2Lifecycle { get; init; } = null!;
            public IStorageDriverFactory? StorageDriverFactory { get; init; }

            // Presentation layer services
            public IProviderRegistry ProviderRegistry { get; init; } = null!;
            public IHistoryManager HistoryManager { get; init; } = null!;
            public IClientManager ClientManager { get; init; } = null!;
            public ISensorManager SensorManager { get; init; } = null!;
            public IOverlayService OverlayService { get; init; } = null!;
            public IContextEngine ContextEngine { get; init; } = null!;
            public IPawnAgentFactory PawnAgentFactory { get; init; } = null!;
            public IGameContextBuilder GameContextBuilder { get; init; } = null!;
            public IResponseDispatcher ResponseDispatcher { get; init; } = null!;
            public IContextKeyRegistry ContextKeyRegistry { get; init; } = null!;
            public IContextKeyProvider ContextKeyProvider { get; init; } = null!;
            public IContextCacheManager CacheManager { get; init; } = null!;
            public IContextDiffTracker DiffTracker { get; init; } = null!;
            public IContextLayerBuilder LayerBuilder { get; init; } = null!;
            public IBudgetScheduler BudgetScheduler { get; init; } = null!;

            // Pipelines
            public IPipeline<AIRequestContext> AIRequestPipeline { get; init; } = null!;
            public IPipeline<NpcChatContext> NpcChatPipeline { get; init; } = null!;
            public IPipeline<ContextBuildContext> ContextBuildPipeline { get; init; } = null!;
            public IPipeline<BusPublishContext> BusPublishPipeline { get; init; } = null!;

            // Resolved GameComponent services (Verse-instantiated, may be null)
            public INpcManager? NpcManager { get; init; }

            // Settings
            public ISettingsProvider SettingsProvider { get; init; } = null!;
        }

        public CompositionResult Compose(ISettingsProvider? settingsProvider, IOpenAISettings? openAISettings)
        {
            // Phase 1: Use injected settings (passed from AICoreMod) — no SL fallback needed
            var resolvedSettings = settingsProvider!;

            // Register settings sub-interfaces so downstream code can resolve them without casting
            if (resolvedSettings is IAgentTickSettings tickSettings)
                RimMindServiceLocator.Register(tickSettings);
            if (resolvedSettings is IOpenAISettings openAISettingsFromProvider)
                RimMindServiceLocator.Register(openAISettingsFromProvider);
            if (resolvedSettings is IApiCredentialSettings apiCredSettings)
                RimMindServiceLocator.Register(apiCredSettings);

            // Phase 2: Register Application and Infrastructure services, capture direct references
            var appBag = Application.DependencyInjection.AddApplicationServices(resolvedSettings);
            var infraBag = Infrastructure.DependencyInjection.AddInfrastructureServices(
                appBag.ToolRegistry, appBag.JsonExtractor, resolvedSettings);

            var logSink = infraBag.LogSink;
            var tickProvider = infraBag.TickProvider;

            PawnDataExtractor.Initialize(logSink);

            var providerRegistry = new ProviderRegistry();
            RimMindServiceLocator.Register<IProviderRegistry>(providerRegistry);

            var historyManager = new HistoryManager(tickProvider);
            RimMindServiceLocator.Register<IHistoryManager>(historyManager);

            var extensionRegistry = new ExtensionRegistry<IAIClientFactory>();
            var clientManager = new ClientManager(resolvedSettings, extensionRegistry);
            RimMindServiceLocator.Register<IClientManager>(clientManager);

            var sensorManager = new SensorManager();
            RimMindServiceLocator.Register<ISensorManager>(sensorManager);

            var overlayService = new OverlayService();
            RimMindServiceLocator.Register<IOverlayService>(overlayService);

            // Phase 3: Create Context layer services (before pipelines, which depend on them)
            var agentBus = appBag.AgentBus;

            // InnerVoiceHandler: subscribes to InnerVoiceEvent for context injection
            var innerVoiceHandler = new InnerVoiceHandler(agentBus, infraBag.TickProvider, logSink);
            innerVoiceHandler.StartListening();
            RimMindServiceLocator.Register(innerVoiceHandler);

            var cacheManager = new ContextCacheManager(logSink);
            var diffTracker = new ContextDiffTracker(logSink);
            var keyProvider = new DefaultContextKeyProvider();
            var layerBuilder = new ContextLayerBuilder(keyProvider, logSink);
            var budgetScheduler = new BudgetScheduler();

            var buildServices = new ContextBuildServices(cacheManager, diffTracker, layerBuilder, budgetScheduler);

            RimMindServiceLocator.Register<IBudgetScheduler>(budgetScheduler);
            RimMindServiceLocator.Register<IContextCacheManager>(cacheManager);
            RimMindServiceLocator.Register<IContextDiffTracker>(diffTracker);
            RimMindServiceLocator.Register<IContextLayerBuilder>(layerBuilder);

            // Phase 4: Resolve GameComponent services (Verse-instantiated, may be null)
            var npcManager = RimMindServiceLocator.Get<INpcManager>();
            var translationService = infraBag.TranslationService;
            var flywheelParameterStore = appBag.ParameterStore;

            var embeddingSnapshotStore = new EmbeddingSnapshotStore();

            var contextEngine = new ContextOrchestrator(
                historyManager,
                npcManager,
                buildServices,
                resolvedSettings,
                translationService,
                flywheelParameterStore,
                logSink,
                embeddingSnapshotStore);
            RimMindServiceLocator.Register<IContextEngine>(contextEngine);
            RimMindServiceLocator.Register<IContextKeyProvider>(keyProvider);
            // ContextOrchestrator implements IContextEngine : IContextBuilder, IContextCache, IContextInvalidation
            // Register derived interfaces so downstream code can resolve them directly
            RimMindServiceLocator.Register<IContextBuilder>(contextEngine);
            RimMindServiceLocator.Register<IContextCache>(contextEngine);

            // Phase 5: Wire pipelines using direct references
            var contextKeyRegistry = new ContextKeyRegistryAdapter();

            var aiRequestPipeline = AIRequestPipelineFactory.Build(
                resolvedSettings,
                appBag.ToolRegistry,
                logSink,
                GetExtensionRegistry<IMiddleware<AIRequestContext>>());

            var npcChatPipeline = NpcChatPipelineFactory.Build(
                contextEngine,
                infraBag.StorageDriverFactory,
                logSink,
                npcManager,
                GetExtensionRegistry<IMiddleware<NpcChatContext>>());

            var contextBuildPipeline = ContextBuildPipelineFactory.Build(
                resolvedSettings,
                cacheManager,
                layerBuilder,
                diffTracker,
                contextKeyRegistry,
                logSink,
                GetExtensionRegistry<IMiddleware<ContextBuildContext>>());

            var busPublishPipeline = BusPublishPipelineFactory.Build(
                evt => agentBus.DispatchAction?.Invoke(evt),
                logSink,
                infraBag.ThreadChecker,
                GetExtensionRegistry<IMiddleware<BusPublishContext>>());
            // SetPipeline is internal on AgentBusImpl — accessible within the same assembly.
            // This is the only remaining 'as' cast for AgentBus; it cannot be moved to the
            // IAgentBus interface because BusPublishContext lives in the Features layer.
            (agentBus as AgentBusImpl)?.SetPipeline(busPublishPipeline);

            // Phase 6: Create remaining Presentation services

            var pawnAgentFactory = new PawnAgentFactory(RimMindServiceLocator.Get<IAgentTickSettings>(), agentBus);
            RimMindServiceLocator.Register<IPawnAgentFactory>(pawnAgentFactory);

            var gameContextBuilder = new GameContextBuilder(
                new PawnContextBuilder(resolvedSettings),
                new MapContextBuilder(resolvedSettings),
                npcManager);
            RimMindServiceLocator.Register<IGameContextBuilder>(gameContextBuilder);

            var responseDispatcher = new ResponseDispatcher(agentBus);
            RimMindServiceLocator.Register<IResponseDispatcher>(responseDispatcher);

            RimMindServiceLocator.Register<IContextKeyRegistry>(contextKeyRegistry);

            ContextKeyRegistry.Initialize(logSink, translationService, keyProvider, npcManager);

            RimMindServiceLocator.Register(GetExtensionRegistry<ISettingsTab>());

            // Register restored extension interfaces with null defaults
            var modCooldownRegistry = new ExtensionRegistry<IModCooldown>();
            modCooldownRegistry.Register(new NullModCooldown());
            RimMindServiceLocator.Register<IExtensionRegistry<IModCooldown>>(modCooldownRegistry);

            var dialogueTriggerRegistry = new ExtensionRegistry<IDialogueTrigger>();
            dialogueTriggerRegistry.Register(new NullDialogueTrigger());
            RimMindServiceLocator.Register<IExtensionRegistry<IDialogueTrigger>>(dialogueTriggerRegistry);

            var incidentListenerRegistry = new ExtensionRegistry<IIncidentExecutedListener>();
            incidentListenerRegistry.Register(new NullIncidentExecutedListener());
            RimMindServiceLocator.Register<IExtensionRegistry<IIncidentExecutedListener>>(incidentListenerRegistry);

            var skipCheckRegistry = new ExtensionRegistry<ISkipCheck>();
            skipCheckRegistry.Register(new NullSkipCheck());
            RimMindServiceLocator.Register<IExtensionRegistry<ISkipCheck>>(skipCheckRegistry);

            // Phase 6: Register built-in client factories
            var clientFactoryRegistry = GetExtensionRegistry<IAIClientFactory>();
            var aiDebugLog = RimMindServiceLocator.Get<IAIDebugLog>();
            var resolvedOpenAISettings = openAISettings ?? RimMindServiceLocator.Get<IOpenAISettings>();
            Infrastructure.DependencyInjection.RegisterBuiltinClientFactories(clientFactoryRegistry, logSink, aiDebugLog, resolvedOpenAISettings);
            RimMindServiceLocator.Register(clientFactoryRegistry);

            // Set AIProviderRegistry default registry (Composition Root wiring)
            Application.Common.Helpers.AIProviderRegistry.DefaultRegistry = clientFactoryRegistry;

            // Phase 7: Initialize Infrastructure static caches with resolved dependencies
            RimMind.Infrastructure.Persistence.StorageDriverFactory.Initialize(
                RimMindServiceLocator.Get<IApiCredentialSettings>(),
                historyManager,
                clientManager,
                npcManager,
                logSink,
                resolvedSettings,
                contextEngine,
                gameContextBuilder,
                responseDispatcher);

            RimMindCoreDebugActions.Initialize(
                resolvedSettings,
                appBag.Queue,
                clientManager,
                aiDebugLog,
                keyProvider,
                contextEngine,
                providerRegistry,
                contextKeyRegistry,
                flywheelParameterStore,
                appBag.Telemetry,
                agentBus,
                historyManager,
                npcManager);

            return new CompositionResult
            {
                // Application
                AgentBus = agentBus,
                ToolRegistry = appBag.ToolRegistry,
                Queue = appBag.Queue,
                Telemetry = appBag.Telemetry,
                ParameterStore = flywheelParameterStore,

                // Infrastructure
                AudioPlayer = infraBag.AudioPlayer,
                TickProvider = tickProvider,
                ThreadChecker = infraBag.ThreadChecker,
                LogSink = logSink,
                TranslationService = translationService,
                MechanismRegistry = infraBag.MechanismRegistry,
                WindowService = infraBag.WindowService,
                Player2Lifecycle = infraBag.Player2Lifecycle,
                StorageDriverFactory = infraBag.StorageDriverFactory,

                // Presentation
                ProviderRegistry = providerRegistry,
                HistoryManager = historyManager,
                ClientManager = clientManager,
                SensorManager = sensorManager,
                OverlayService = overlayService,
                ContextEngine = contextEngine,
                PawnAgentFactory = pawnAgentFactory,
                GameContextBuilder = gameContextBuilder,
                ResponseDispatcher = responseDispatcher,
                ContextKeyRegistry = contextKeyRegistry,
                ContextKeyProvider = keyProvider,
                CacheManager = cacheManager,
                DiffTracker = diffTracker,
                LayerBuilder = layerBuilder,
                BudgetScheduler = budgetScheduler,

                // Pipelines
                AIRequestPipeline = aiRequestPipeline,
                NpcChatPipeline = npcChatPipeline,
                ContextBuildPipeline = contextBuildPipeline,
                BusPublishPipeline = busPublishPipeline,

                // GameComponent
                NpcManager = npcManager,

                // Settings
                SettingsProvider = resolvedSettings
            };
        }

        private IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension
        {
            var registry = RimMindServiceLocator.Get<IExtensionRegistry<T>>();
            if (registry != null) return registry;
            var newRegistry = new ExtensionRegistry<T>();
            RimMindServiceLocator.Register(newRegistry);
            return newRegistry;
        }
    }
}
