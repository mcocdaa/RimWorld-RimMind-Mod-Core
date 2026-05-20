using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Runtime;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Presentation.Settings;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Features.Agent.Modes;
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
using RimMind.Presentation.Pipeline.AI;
using RimMind.Presentation.Pipeline.Context;
using RimMind.Presentation.Pipeline.Npc;
using RimMind.Presentation.Sensor;
using Verse;

namespace RimMind.Presentation.Runtime
{
    public sealed class RimMindRuntime : IRimMindRuntime
    {
        private static RimMindRuntime? _instance;
        private static readonly object _initLock = new object();

        public static RimMindRuntime Instance => _instance
            ?? throw new InvalidOperationException("[RimMind-Core] RimMindRuntime not initialized. Call Initialize() first.");

        public IAgentBus AgentBus { get; internal set; }
        public IContextEngine ContextEngine { get; internal set; }
        public IHistoryManager HistoryManager { get; internal set; }
        public IClientManager ClientManager { get; internal set; }
        public IAudioPlayer AudioPlayer { get; internal set; }
        public IProviderRegistry ProviderRegistry { get; internal set; }
        public IOverlayService OverlayService { get; internal set; }
        public IAIRequestQueue QueueImpl { get; private set; }
        public IAIRequestQueue Queue => QueueImpl;
        public ITelemetryCollector Telemetry { get; private set; }
        public IToolRegistry ToolRegistry { get; private set; }
        public IGameMechanismRegistry MechanismRegistry { get; private set; }
        public IStorageDriverFactory? StorageDriverFactory { get; private set; }
        public int MaxToolCallDepth { get; set; } = 3;
        private IPipeline<AIRequestContext>? _aiRequestPipeline;
        public IPipeline<AIRequestContext> AIRequestPipeline => _aiRequestPipeline!;

        private IPipeline<NpcChatContext>? _npcChatPipeline;
        public IPipeline<NpcChatContext> NpcChatPipeline => _npcChatPipeline!;

        private IPipeline<ContextBuildContext>? _contextBuildPipeline;
        public IPipeline<ContextBuildContext> ContextBuildPipeline => _contextBuildPipeline!;

        private IPipeline<BusPublishContext>? _busPublishPipeline;
        public IPipeline<BusPublishContext> BusPublishPipeline => _busPublishPipeline!;

        private readonly ConcurrentDictionary<Type, object> _registries = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<string, IParameterTuner> _parameterTuners = new ConcurrentDictionary<string, IParameterTuner>();
#pragma warning disable CS0618 // ISensorProvider is dead code (no implementations) but kept for public API surface
        private readonly ConcurrentDictionary<string, ISensorProvider> _sensorProviders = new ConcurrentDictionary<string, ISensorProvider>();
#pragma warning restore CS0618

        private AgentBusCoreSubscriber? _coreSubscriber;
        private volatile Func<Pawn, AgentIdentity?>? _agentIdentityProvider;
        private volatile IAgentActionBridge _agentActionBridge = Application.Common.Defaults.NullAgentActionBridge.Instance;
        private volatile bool _isShutdown;

        private ISettingsProvider? _settingsProvider;
        private ILogSink? _logSink;
        private IThreadChecker? _threadChecker;
        private ITickProvider? _tickProvider;

        public IReadOnlyList<IParameterTuner> ParameterTunersList => _parameterTuners.Values.ToList();
#pragma warning disable CS0618 // ISensorProvider is dead code (no implementations) but kept for public API surface
        public IReadOnlyList<ISensorProvider> SensorProvidersList => _sensorProviders.Values.ToList();
#pragma warning restore CS0618

        public Func<Pawn, AgentIdentity?>? AgentIdentityProvider => _agentIdentityProvider;
        public IAgentActionBridge AgentActionBridge => _agentActionBridge;
        public bool IsShutdown => _isShutdown;

        private RimMindRuntime()
        {
            _settingsProvider = RimMindServiceLocator.Get<ISettingsProvider>();

            Application.DependencyInjection.AddApplicationServices(
                _settingsProvider);
            Infrastructure.DependencyInjection.AddInfrastructureServices();

            _logSink = RimMindServiceLocator.Get<ILogSink>();
            _threadChecker = RimMindServiceLocator.Get<IThreadChecker>();
            _tickProvider = RimMindServiceLocator.Get<ITickProvider>();

            PawnDataExtractor.Initialize(_logSink);

            var providerRegistry = new ProviderRegistry();
            RimMindServiceLocator.Register<IProviderRegistry>(providerRegistry);

            var historyManager = new HistoryManager(_tickProvider);
            RimMindServiceLocator.Register<IHistoryManager>(historyManager);

            var clientManager = new ClientManager(_settingsProvider,
                GetExtensionRegistry<IAIClientFactory>());
            RimMindServiceLocator.Register<IClientManager>(clientManager);

            var sensorManager = new SensorManager();
            RimMindServiceLocator.Register<ISensorManager>(sensorManager);

            var overlayService = new OverlayService();
            RimMindServiceLocator.Register<IOverlayService>(overlayService);

            // WindowService and AgentActiveChecker now registered in Infrastructure DI
            // IWindowService and IAgentActiveChecker are available via ServiceLocator

            AgentBus = RimMindServiceLocator.Get<IAgentBus>()!;
            var busImpl = AgentBus as AgentBusImpl;
            _aiRequestPipeline = AIRequestPipelineFactory.Build(
                _settingsProvider!,
                _logSink,
                GetExtensionRegistry<IMiddleware<AIRequestContext>>());

            _npcChatPipeline = NpcChatPipelineFactory.Build(
                GetExtensionRegistry<IMiddleware<NpcChatContext>>());

            _contextBuildPipeline = ContextBuildPipelineFactory.Build(
                _settingsProvider!,
                GetExtensionRegistry<IMiddleware<ContextBuildContext>>());

            _busPublishPipeline = BusPublishPipelineFactory.Build(
                evt => busImpl?.DispatchToHandlers(evt),
                _logSink,
                _threadChecker,
                GetExtensionRegistry<IMiddleware<BusPublishContext>>());
            busImpl?.SetPipeline(_busPublishPipeline);

            ProviderRegistry = providerRegistry;
            HistoryManager = historyManager;
            ClientManager = clientManager;
            AudioPlayer = RimMindServiceLocator.Get<IAudioPlayer>()!;
            OverlayService = overlayService;
            ToolRegistry = RimMindServiceLocator.Get<IToolRegistry>()!;
            MechanismRegistry = RimMindServiceLocator.Get<IGameMechanismRegistry>()!;
            StorageDriverFactory = RimMindServiceLocator.Get<IStorageDriverFactory>();
            Telemetry = RimMindServiceLocator.Get<ITelemetryCollector>()!;
            QueueImpl = RimMindServiceLocator.Get<IAIRequestQueue>()!;

            var cacheManager = new ContextCacheManager(_logSink);
            var diffTracker = new ContextDiffTracker(_logSink);
            var keyProvider = new DefaultContextKeyProvider();
            var layerBuilder = new ContextLayerBuilder(keyProvider, _logSink);
            var budgetScheduler = new BudgetScheduler();

            // NpcManager is a GameComponent — Verse instantiates it automatically.
            // It self-registers into RimMindServiceLocator via its constructor.
            var npcManager = RimMindServiceLocator.Get<INpcManager>();
            var translationService = RimMindServiceLocator.Get<ITranslationService>();
            var flywheelParameterStore = RimMindServiceLocator.Get<IFlywheelParameterStore>();

            ContextEngine = new ContextOrchestrator(
                HistoryManager,
                npcManager,
                cacheManager,
                diffTracker,
                layerBuilder,
                budgetScheduler,
                _settingsProvider!,
                translationService!,
                flywheelParameterStore!,
                _logSink);
            RimMindServiceLocator.Register<IContextEngine>(ContextEngine);
            RimMindServiceLocator.Register<IContextKeyProvider>(keyProvider);

            // AgentActiveChecker now registered in Infrastructure DI

            RimMindServiceLocator.Register<IRimMindRuntime>(this);

            var pawnAgentFactory = new PawnAgentFactory(null, AgentBus);
            RimMindServiceLocator.Register<IPawnAgentFactory>(pawnAgentFactory);
            RimMindServiceLocator.Register<IGameContextBuilder>(new GameContextBuilder(
                RimMindServiceLocator.Get<IContextCalibrationSettings>(),
                npcManager));

            var responseDispatcher = new ResponseDispatcher(AgentBus);
            RimMindServiceLocator.Register<IResponseDispatcher>(responseDispatcher);

            var contextKeyRegistry = new ContextKeyRegistryAdapter();
            RimMindServiceLocator.Register<IContextKeyRegistry>(contextKeyRegistry);

            ContextKeyRegistry.Initialize(_logSink, translationService, keyProvider, npcManager);

            RimMindServiceLocator.Register(GetExtensionRegistry<ISettingsTab>());

            var clientFactoryRegistry = GetExtensionRegistry<IAIClientFactory>();
            var aiDebugLog = RimMindServiceLocator.Get<IAIDebugLog>();
            var openAISettings = RimMindServiceLocator.Get<IOpenAISettings>();
            Infrastructure.DependencyInjection.RegisterBuiltinClientFactories(clientFactoryRegistry, _logSink, aiDebugLog, openAISettings);
            RimMindServiceLocator.Register(clientFactoryRegistry);

            // Set ProviderHelper default registry (Composition Root wiring)
            Application.Common.Helpers.ProviderHelper.DefaultRegistry = clientFactoryRegistry;

            // Initialize Infrastructure static caches with resolved dependencies
            var gameContextBuilder = RimMindServiceLocator.Get<IGameContextBuilder>();
            RimMind.Infrastructure.Persistence.StorageDriverFactory.Initialize(
                _settingsProvider as IApiCredentialSettings,
                HistoryManager,
                ClientManager,
                npcManager,
                _logSink,
                _settingsProvider,
                ContextEngine,
                gameContextBuilder,
                responseDispatcher);

            RimMindCoreDebugActions.Initialize(
                _settingsProvider,
                QueueImpl,
                ClientManager,
                aiDebugLog,
                keyProvider,
                ContextEngine,
                ProviderRegistry,
                contextKeyRegistry,
                flywheelParameterStore,
                Telemetry,
                AgentBus,
                HistoryManager,
                npcManager);

        }

        public static void Initialize()
        {
            lock (_initLock)
            {
                if (_instance != null) return;
                _instance = new RimMindRuntime();
                _instance.RegisterBuiltinModes();
                _instance.RegisterCoreSubscribers();
                Log.Message("[RimMind-Core] Runtime initialized");
            }
        }

        public void Shutdown()
        {
            if (_isShutdown) return;
            _isShutdown = true;
            (Telemetry as IDisposable)?.Dispose();
            ContextEngine.Dispose();
            RimMindServiceLocator.Get<IPlayer2Lifecycle>()?.StopHealthCheck();
        }

        public static void ResetInstance()
        {
            lock (_initLock)
            {
                if (_instance != null)
                {
                    _instance.Shutdown();
                    if (_instance.AgentBus is AgentBusImpl busImpl)
                        busImpl.ClearAllSubscribers();
                    _instance._parameterTuners.Clear();
                    _instance._sensorProviders.Clear();
                    _instance._registries.Clear();
                    _instance._agentIdentityProvider = null;
                    _instance._agentActionBridge = Application.Common.Defaults.NullAgentActionBridge.Instance;
                }
                RimMindServiceLocator.Reset();
                ContextKeyRegistry.ResetCache();
                ContextKeyRegistry.Clear();
                GameContextBuilder.ResetCache();
                _instance = null;
            }
        }

        public IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension
        {
            return (IExtensionRegistry<T>)_registries.GetOrAdd(typeof(T),
                _ => new ExtensionRegistry<T>());
        }

        public void AddMiddleware<TContext>(IMiddleware<TContext> middleware) where TContext : IPipelineContext
        {
            if (middleware == null) return;
            bool added = false;
            if (_aiRequestPipeline is MutablePipeline<AIRequestContext> aiPipe && middleware is IMiddleware<AIRequestContext> aiMw)
            { aiPipe.Use(aiMw); added = true; }
            else if (_npcChatPipeline is MutablePipeline<NpcChatContext> npcPipe && middleware is IMiddleware<NpcChatContext> npcMw)
            { npcPipe.Use(npcMw); added = true; }
            else if (_contextBuildPipeline is MutablePipeline<ContextBuildContext> ctxPipe && middleware is IMiddleware<ContextBuildContext> ctxMw)
            { ctxPipe.Use(ctxMw); added = true; }
            else if (_busPublishPipeline is MutablePipeline<BusPublishContext> busPipe && middleware is IMiddleware<BusPublishContext> busMw)
            { busPipe.Use(busMw); added = true; }

            if (!added)
            {
                _logSink?.Warning($"[RimMindRuntime] AddMiddleware: no pipeline found for TContext={typeof(TContext).Name}, middleware={middleware.Name}");
            }
        }

        public void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider)
            => _agentIdentityProvider = provider;

        public AgentIdentity? GetAgentIdentity(Pawn pawn)
            => _agentIdentityProvider?.Invoke(pawn);

        public void RegisterAgentActionBridge(IAgentActionBridge bridge)
        {
            _agentActionBridge = bridge;
            RimMindServiceLocator.Register(bridge);
        }

        public IAgentActionBridge GetAgentActionBridge() => _agentActionBridge;

        public void RegisterParameterTuner(IParameterTuner tuner)
            => _parameterTuners[tuner.TunerId] = tuner;

#pragma warning disable CS0618 // ISensorProvider is dead code (no implementations) but kept for public API surface
        public void RegisterSensorProvider(ISensorProvider provider)
        {
            _sensorProviders[provider.SensorId] = provider;
            var sensorManager = RimMindServiceLocator.Get<ISensorManager>() as Sensor.SensorManager;
            sensorManager?.RegisterProvider(provider);
        }
#pragma warning restore CS0618

        public void UnregisterSensorProvider(string sensorId)
        {
            _sensorProviders.TryRemove(sensorId, out _);
            var sensorManager = RimMindServiceLocator.Get<ISensorManager>() as Sensor.SensorManager;
            sensorManager?.UnregisterProvider(sensorId);
            ContextKeyRegistry.Unregister($"sensor_{sensorId}");
        }

        public IAIClient? GetClient() => ClientManager.GetClient();
        public void InvalidateClientCache() => ClientManager.InvalidateCache();
        public IAIClient? GetPlayer2Client() => ClientManager.GetPlayer2Client();
        public ISettingsProvider? GetSettingsProvider() => _settingsProvider;

        public T? GetService<T>() where T : class => RimMindServiceLocator.Get<T>();

        public void RegisterService<T>(T instance) where T : class => RimMindServiceLocator.Register<T>(instance);

        private void RegisterBuiltinModes()
        {
            var modeRegistry = GetExtensionRegistry<IAgentMode>();
            var tickProvider = _tickProvider
                ?? RimMindServiceLocator.Get<ITickProvider>()
                ?? throw new InvalidOperationException("ITickProvider not registered");
            modeRegistry.Register(new ReactiveAgentMode());
            modeRegistry.Register(new ProactiveAgentMode(tickProvider));
        }

        private void RegisterCoreSubscribers()
        {
            var logSink = _logSink ?? RimMindServiceLocator.Get<ILogSink>()!;
            var agentBus = AgentBus;
            _coreSubscriber = new AgentBusCoreSubscriber(agentBus, logSink);
        }
    }

}
