using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Diagnostics;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Pipeline.Bus;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Infrastructure;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Settings;
using RimMind.Presentation.Runtime.Composition;

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
            public IPawnAgentFactoryVerse PawnAgentFactory { get; init; } = null!;
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

            SettingsComposition.Register(resolvedSettings, openAISettings);

            // Phase 2: Register Application and Infrastructure services, capture direct references
            var appBag = Application.DependencyInjection.AddApplicationServices(resolvedSettings);
            SettingsComposition.RegisterApplicationServices(appBag);

            var infraBag = Infrastructure.DependencyInjection.AddInfrastructureServices(
                appBag.ToolRegistry, appBag.JsonExtractor, resolvedSettings);
            SettingsComposition.RegisterInfrastructureServices(infraBag);

            var logSink = infraBag.LogSink;
            var tickProvider = infraBag.TickProvider;
            var agentBus = appBag.AgentBus;
            var flywheelParameterStore = appBag.ParameterStore;
            var translationService = infraBag.TranslationService;

            // H2: Register all 17 built-in Mechanisms
            var mechanismRegistry = infraBag.MechanismRegistry;
            ToolMechanismComposition.RegisterAllMechanisms(mechanismRegistry);

            PawnDataExtractor.Initialize(logSink);

            var clientServices = ClientComposition.RegisterClientManager(resolvedSettings);
            var uiServices = UiComposition.RegisterServices();
            var npcManager = RimMindServiceLocator.TryGet<INpcManager>();

            var contextServices = ContextComposition.Register(
                resolvedSettings,
                agentBus,
                tickProvider,
                logSink,
                npcManager,
                translationService,
                flywheelParameterStore);

            var busPublishPipeline = BusPublishPipelineFactory.Build(
                evt => agentBus.DispatchAction?.Invoke(evt),
                logSink,
                infraBag.ThreadChecker,
                CompositionRegistry.GetExtensionRegistry<IMiddleware<BusPublishContext>>());
            agentBus.SetPipeline(busPublishPipeline);

            // Phase 5b: Build unified pipeline
            // L5: Create AIResponseAnalyzer for context feedback (RelevanceLearner created in Phase 3)
            var responseAnalyzer = new AIResponseAnalyzer();

            var unifiedPipeline = UnifiedRequestPipelineFactory.Build(
                appBag.ToolRegistry,
                logSink,
                npcManager,
                contextServices.ContextEngine,
                appBag.Telemetry,
                resolvedSettings,
                CompositionRegistry.GetExtensionRegistry<IMiddleware<LlmRequestContext>>(),
                contextServices.RelevanceLearner,
                responseAnalyzer,
                RimMindServiceLocator.TryGet<IAIRequestTraceLog>());

            var actionExecutor = ToolMechanismComposition.RegisterActionExecutor(infraBag.MechanismRegistry);
            var psychologyWatcher = RimMindServiceLocator.TryGet<IPsychologyWatcher>();
            var agentServices = AgentComposition.RegisterAgents(
                resolvedSettings,
                agentBus,
                actionExecutor,
                contextServices.InnerVoiceHandler,
                psychologyWatcher,
                tickProvider,
                logSink,
                npcManager);

            RimMindServiceLocator.Register<IContextKeyRegistry>(contextServices.ContextKeyRegistry);

            SettingsComposition.RegisterDefaultExtensionRegistries();

            var aiDebugLog = ClientComposition.RegisterBuiltinClientFactories(
                clientServices.ClientFactoryRegistry,
                logSink,
                openAISettings);
            var remoteSync = ClientComposition.RegisterRemoteSync(logSink);
            UiComposition.RegisterRemoteSyncSettingsTab(remoteSync.Settings, remoteSync.Service);

            UiComposition.InitializeDebugActions(
                resolvedSettings,
                appBag.Queue,
                clientServices.ClientManager,
                aiDebugLog,
                contextServices.ContextKeyProvider,
                contextServices.ContextEngine,
                contextServices.ProviderRegistry,
                contextServices.ContextKeyRegistry,
                flywheelParameterStore,
                appBag.Telemetry,
                agentBus,
                contextServices.HistoryManager,
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
                ProviderRegistry = contextServices.ProviderRegistry,
                HistoryManager = contextServices.HistoryManager,
                ClientManager = clientServices.ClientManager,
                SensorManager = uiServices.SensorManager,
                OverlayService = uiServices.OverlayService,
                ContextEngine = contextServices.ContextEngine,
                PawnAgentFactory = agentServices.PawnAgentFactory,
                GameContextBuilder = agentServices.GameContextBuilder,
                ResponseDispatcher = agentServices.ResponseDispatcher,
                ContextKeyRegistry = contextServices.ContextKeyRegistry,
                ContextKeyProvider = contextServices.ContextKeyProvider,
                CacheManager = contextServices.CacheManager,
                DiffTracker = contextServices.DiffTracker,
                LayerBuilder = contextServices.LayerBuilder,
                BudgetScheduler = contextServices.BudgetScheduler,
                RelevanceTable = contextServices.RelevanceTable,
                RelevanceLearner = contextServices.RelevanceLearner,

                // Pipelines
                BusPublishPipeline = busPublishPipeline,
                UnifiedPipeline = unifiedPipeline,

                // GameComponent
                NpcManager = npcManager,

                // Settings
                SettingsProvider = resolvedSettings
            };
        }

    }
}
