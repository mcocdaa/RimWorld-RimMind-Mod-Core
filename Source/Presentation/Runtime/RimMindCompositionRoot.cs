using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent.Modes;
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
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Perception;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Features.Agent.InnerVoice;
using RimMind.Application.Features.Agent.Psychology;
using RimMind.Application.Features.Agent.Social;
using RimMind.Infrastructure.Psychology;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Application.Features.Pipeline.Bus;
using RimMind.Application.Features.Registry;
using RimMind.Infrastructure;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Job;
using RimMind.Infrastructure.Mechanisms.Pawn.Draft;
using RimMind.Infrastructure.Mechanisms.Pawn.Work;
using RimMind.Infrastructure.Mechanisms.Pawn.Equipment;
using RimMind.Infrastructure.Mechanisms.Pawn.Interaction;
using RimMind.Infrastructure.Mechanisms.Pawn.Recruit;
using RimMind.Infrastructure.Mechanisms.Pawn.Thought;
using RimMind.Infrastructure.Mechanisms.Pawn.Inspiration;
using RimMind.Infrastructure.Mechanisms.Pawn.MentalState;
using RimMind.Infrastructure.Mechanisms.Pawn.Health;
using RimMind.Infrastructure.Mechanisms.Pawn.Relations;
using RimMind.Infrastructure.Mechanisms.Pawn.Skill;
using RimMind.Infrastructure.Mechanisms.Pawn.Need;
using RimMind.Infrastructure.Mechanisms.Map.Wealth;
using RimMind.Infrastructure.Mechanisms.World.Faction;
using RimMind.Infrastructure.Mechanisms.World.Storyteller;
using RimMind.Infrastructure.Mechanisms.World.ChoiceLetter;
using RimMind.Infrastructure.Agent;
using RimMind.Infrastructure.Social;
using RimMind.Infrastructure.UI;
using RimMind.Application.Common.Models.Agent;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Context;
using RimMind.Presentation.Llm;
using RimMind.Presentation.Settings;
using RimMind.Presentation.Sensor;
using Verse;

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
            public IRelevanceTable RelevanceTable { get; init; } = null!;
            public IRelevanceLearner RelevanceLearner { get; init; } = null!;

            // Pipelines
            public IPipeline<BusPublishContext> BusPublishPipeline { get; init; } = null!;
            public IPipeline<LlmRequestContext> UnifiedPipeline { get; init; } = null!;

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

            // H2: Register all 17 built-in Mechanisms
            var mechanismRegistry = infraBag.MechanismRegistry;
            RegisterAllMechanisms(mechanismRegistry);

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

            // L3: Instance-based registries replacing static classes
            var keyRegistryImpl = new ContextKeyRegistryImpl(logSink);
            var relevanceTableImpl = new RelevanceTableImpl();
            var relevanceLearner = new RelevanceLearner(tickProvider);
            var budgetScheduler = new BudgetScheduler(relevanceTableImpl, relevanceLearner, tickProvider, cacheManager.EmbedCache);
            var providerCache = new ProviderCache(agentBus, logSink, tickProvider);

            var buildServices = new ContextBuildServices(cacheManager, diffTracker, layerBuilder, budgetScheduler);

            RimMindServiceLocator.Register<IBudgetScheduler>(budgetScheduler);
            RimMindServiceLocator.Register<IContextCacheManager>(cacheManager);
            RimMindServiceLocator.Register<IContextDiffTracker>(diffTracker);
            RimMindServiceLocator.Register<IContextLayerBuilder>(layerBuilder);
            RimMindServiceLocator.Register<IContextKeyRegistry>(keyRegistryImpl);
            RimMindServiceLocator.Register<IRelevanceTable>(relevanceTableImpl);
            RimMindServiceLocator.Register<IRelevanceLearner>(relevanceLearner);

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
                embeddingSnapshotStore,
                keyRegistryImpl,
                relevanceTableImpl,
                providerCache,
                tickProvider);
            RimMindServiceLocator.Register<IContextEngine>(contextEngine);
            RimMindServiceLocator.Register<IContextKeyProvider>(keyProvider);
            // ContextOrchestrator implements IContextEngine : IContextBuilder, IContextCache, IContextInvalidation
            // Register derived interfaces so downstream code can resolve them directly
            RimMindServiceLocator.Register<IContextBuilder>(contextEngine);
            RimMindServiceLocator.Register<IContextCache>(contextEngine);

            // Phase 5: Wire pipelines using direct references
            var contextKeyRegistry = keyRegistryImpl as IContextKeyRegistry;

            var busPublishPipeline = BusPublishPipelineFactory.Build(
                evt => agentBus.DispatchAction?.Invoke(evt),
                logSink,
                infraBag.ThreadChecker,
                GetExtensionRegistry<IMiddleware<BusPublishContext>>());
            agentBus.SetPipeline(busPublishPipeline);

            // Phase 5b: Build unified pipeline
            // L5: Create AIResponseAnalyzer for context feedback (RelevanceLearner created in Phase 3)
            var responseAnalyzer = new AIResponseAnalyzer();

            var unifiedPipeline = UnifiedRequestPipelineFactory.Build(
                appBag.ToolRegistry,
                logSink,
                npcManager,
                contextEngine,
                appBag.Telemetry,
                resolvedSettings,
                GetExtensionRegistry<IMiddleware<LlmRequestContext>>(),
                relevanceLearner,
                responseAnalyzer);

            // Phase 6: Create remaining Presentation services

            var actionExecutor = new MechanismActionExecutor(infraBag.MechanismRegistry);
            RimMindServiceLocator.Register<IActionExecutor>(actionExecutor);

            RimMindServiceLocator.Register<IAgentIdentityProvider>(new AgentIdentityProviderAdapter());

            var pawnAgentFactory = new PawnAgentFactory(RimMindServiceLocator.Get<IAgentTickSettings>(), agentBus, actionExecutor, logSink,
                GetExtensionRegistry<IPerceptionSource>());
            RimMindServiceLocator.Register<IPawnAgentFactory>(pawnAgentFactory);

            var gameContextBuilder = new GameContextBuilder(
                new PawnContextBuilder(resolvedSettings),
                new MapContextBuilder(resolvedSettings),
                npcManager);
            RimMindServiceLocator.Register<IGameContextBuilder>(gameContextBuilder);

            var responseDispatcher = new ResponseDispatcher(agentBus);
            RimMindServiceLocator.Register<IResponseDispatcher>(responseDispatcher);

            RimMindServiceLocator.Register<IContextKeyRegistry>(contextKeyRegistry);

            RimMindServiceLocator.Register(GetExtensionRegistry<ISettingsTab>());

            // Register built-in RemoteSync settings tab
            var settingsTabRegistry = RimMindServiceLocator.Get<IExtensionRegistry<ISettingsTab>>();
            settingsTabRegistry?.Register(new RimMind.Presentation.UI.RemoteSyncSettingsUI());

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

            // Register default mode transition policy (allows all transitions)
            var modePolicyRegistry = GetExtensionRegistry<IModeTransitionPolicy>();
            modePolicyRegistry.Register(new DefaultModeTransitionPolicy());
            RimMindServiceLocator.Register(modePolicyRegistry);

            // Phase 6: Register built-in client factories
            // NOTE: When remote + local clients are both configured, use HybridAIClient
            // (Infrastructure.Services.Clients.Hybrid) to get remote-first with local fallback
            // on retryable errors (transient, circuit-open, timeout).
            var clientFactoryRegistry = GetExtensionRegistry<IAIClientFactory>();
            var aiDebugLog = RimMindServiceLocator.Get<IAIDebugLog>();
            var resolvedOpenAISettings = openAISettings ?? RimMindServiceLocator.Get<IOpenAISettings>();
            Infrastructure.DependencyInjection.RegisterBuiltinClientFactories(clientFactoryRegistry, logSink, aiDebugLog, resolvedOpenAISettings);
            RimMindServiceLocator.Register(clientFactoryRegistry);

            // Set AIProviderRegistry default registry (Composition Root wiring)
            Application.Common.Helpers.AIProviderRegistry.DefaultRegistry = clientFactoryRegistry;

            // Phase 7: Initialize Infrastructure static caches with resolved dependencies
            // K-phase: Register RemoteSyncService for submodules (replaces IStorageDriver)
            var remoteSyncSettings = new RimMind.Domain.Settings.RemoteSyncSettings();
            RimMindServiceLocator.Register(remoteSyncSettings);
            var remoteBackend = RimMindServiceLocator.Get<RimMind.Domain.Storage.IRemoteBackend>();
            var remoteSyncOrchestrator = new RimMind.Application.Features.Storage.RemoteSyncOrchestrator(
                remoteBackend, remoteSyncSettings, logSink);
            var remoteSyncService = new RimMind.Infrastructure.Services.Storage.RemoteSyncService(remoteSyncOrchestrator);
            RimMindServiceLocator.Register<RimMind.Application.Common.Interfaces.Storage.IRemoteSyncService>(remoteSyncService);

            // Social & Emergence services
            var informationDiffuser = new DefaultInformationDiffuser(agentBus, tickProvider);
            RimMindServiceLocator.Register<IInformationDiffuser>(informationDiffuser);

            var socialEventOrganizer = new DefaultSocialEventOrganizer(tickProvider, agentBus);
            RimMindServiceLocator.Register<ISocialEventOrganizer>(socialEventOrganizer);

            var psychologyWatcher = RimMindServiceLocator.Get<IPsychologyWatcher>();
            var traitEvolutionEngine = new DefaultTraitEvolutionEngine(tickProvider, psychologyWatcher, agentBus);
            RimMindServiceLocator.Register<ITraitEvolutionEngine>(traitEvolutionEngine);

            var sleepDetector = new VersePawnSleepDetector();
            RimMindServiceLocator.Register<ISleepDetector>(sleepDetector);

            var dreamGenerator = new DefaultDreamGenerator(tickProvider, sleepDetector, agentBus);
            RimMindServiceLocator.Register<IDreamGenerator>(dreamGenerator);

            var traitEvolver = new VerseTraitEvolver();
            RimMindServiceLocator.Register<ITraitEvolver>(traitEvolver);

            var thoughtInjector = RimMindServiceLocator.Get<IThoughtInjector>();
            if (thoughtInjector != null)
            {
                var dreamThoughtInjector = new VerseDreamThoughtInjector(thoughtInjector);
                RimMindServiceLocator.Register<IDreamThoughtInjector>(dreamThoughtInjector);
            }

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
                npcManager,
                appBag.ToolRegistry,
                infraBag.MechanismRegistry);

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
                RelevanceTable = relevanceTableImpl,
                RelevanceLearner = relevanceLearner,

                // Pipelines
                BusPublishPipeline = busPublishPipeline,
                UnifiedPipeline = unifiedPipeline,

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

        /// <summary>
        /// H2: Register all 17 built-in Mechanisms into the GameMechanismRegistry.
        /// Each Mechanism auto-registers its ToolHandlers (query/set/toggle/trigger/list) via GameMechanismRegistry.Register.
        /// </summary>
        private static void RegisterAllMechanisms(IGameMechanismRegistry registry)
        {
            // Pawn mechanisms (13)
            registry.Register(new JobMechanism());
            registry.Register(new DraftMechanism());
            registry.Register(new WorkMechanism());
            registry.Register(new EquipmentMechanism());
            registry.Register(new InteractionMechanism());
            registry.Register(new RecruitMechanism());
            registry.Register(new ThoughtMechanism());
            registry.Register(new InspirationMechanism());
            registry.Register(new MentalStateMechanism());
            registry.Register(new HealthMechanism());
            registry.Register(new RelationsMechanism());
            registry.Register(new SkillMechanism());
            registry.Register(new NeedMechanism());

            // Map mechanisms (1)
            registry.Register(new WealthMechanism());

            // World mechanisms (3)
            registry.Register(new FactionMechanism());
            registry.Register(new StorytellerMechanism());
            registry.Register(new ChoiceLetterMechanism());
        }

        /// <summary>
        /// Adapter that wraps RimMindRuntime.GetAgentIdentity as IAgentIdentityProvider.
        /// Registered in ServiceLocator so Infrastructure patches can resolve agent identity
        /// without depending on Presentation-layer RimMindAPI.Ext.
        /// </summary>
        private sealed class AgentIdentityProviderAdapter : IAgentIdentityProvider
        {
            public AgentIdentity? GetAgentIdentity(object pawn)
                => RimMindRuntime.Instance.GetAgentIdentity((Pawn)pawn);
        }
    }
}
