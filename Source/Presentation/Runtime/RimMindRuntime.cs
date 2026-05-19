using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
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
using RimMind.Infrastructure.Services.Clients.OpenAI;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Infrastructure.UI;
using RimMind.Infrastructure.Verse;
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
        [Obsolete("Use AgentBus instead")]
        public IAgentBus EventBus { get; internal set; }
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
        private readonly ConcurrentDictionary<string, ISensorProvider> _sensorProviders = new ConcurrentDictionary<string, ISensorProvider>();

        private AgentBusCoreSubscriber? _coreSubscriber;
        private volatile Func<Pawn, AgentIdentity?>? _agentIdentityProvider;
        private volatile IAgentActionBridge _agentActionBridge = Application.Common.Defaults.NullAgentActionBridge.Instance;
        private volatile bool _isShutdown;

        private readonly ISettingsProvider? _settingsProvider;
        private readonly ILogSink? _logSink;
        private readonly IThreadChecker? _threadChecker;
        private readonly ITickProvider? _tickProvider;

        public IReadOnlyList<IParameterTuner> ParameterTunersList => _parameterTuners.Values.ToList();
        public IReadOnlyList<ISensorProvider> SensorProvidersList => _sensorProviders.Values.ToList();

        public Func<Pawn, AgentIdentity?>? AgentIdentityProvider => _agentIdentityProvider;
        public IAgentActionBridge AgentActionBridge => _agentActionBridge;
        public bool IsShutdown => _isShutdown;

        private RimMindRuntime()
        {
            _settingsProvider = RimMindServiceLocator.Get<ISettingsProvider>();
            _logSink = RimMindServiceLocator.Get<ILogSink>();
            _threadChecker = RimMindServiceLocator.Get<IThreadChecker>();
            _tickProvider = RimMindServiceLocator.Get<ITickProvider>();

            Application.DependencyInjection.AddApplicationServices(
                _settingsProvider);
            Infrastructure.DependencyInjection.AddInfrastructureServices();

            var providerRegistry = new ProviderRegistry();
            RimMindServiceLocator.Register<IProviderRegistry>(providerRegistry);

            var historyManager = new HistoryManager();
            RimMindServiceLocator.Register<IHistoryManager>(historyManager);

            var clientManager = new ClientManager();
            RimMindServiceLocator.Register<IClientManager>(clientManager);

            var sensorManager = new SensorManager();
            RimMindServiceLocator.Register<ISensorManager>(sensorManager);

            var overlayService = new OverlayService();
            RimMindServiceLocator.Register<IOverlayService>(overlayService);

            RimMindServiceLocator.Register<IWindowService>(new WindowService());

            AgentBus = RimMindServiceLocator.Get<IAgentBus>()!;
            var busImpl = AgentBus as AgentBusImpl;
            _aiRequestPipeline = AIRequestPipelineFactory.Build(
                _settingsProvider!,
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

            ProviderRegistry = RimMindServiceLocator.Get<IProviderRegistry>()!;
            HistoryManager = RimMindServiceLocator.Get<IHistoryManager>()!;
            ClientManager = RimMindServiceLocator.Get<IClientManager>()!;
            AudioPlayer = RimMindServiceLocator.Get<IAudioPlayer>()!;
            OverlayService = RimMindServiceLocator.Get<IOverlayService>()!;
            ToolRegistry = RimMindServiceLocator.Get<IToolRegistry>()!;
            MechanismRegistry = RimMindServiceLocator.Get<IGameMechanismRegistry>()!;
            Telemetry = RimMindServiceLocator.Get<ITelemetryCollector>()!;
            QueueImpl = RimMindServiceLocator.Get<IAIRequestQueue>()!;

            var cacheManager = new ContextCacheManager(_logSink);
            var diffTracker = new ContextDiffTracker(_logSink);
            var keyProvider = new DefaultContextKeyProvider();
            var layerBuilder = new ContextLayerBuilder(keyProvider, _logSink);
            var budgetScheduler = new BudgetScheduler();

            var npcManager = RimMindServiceLocator.Get<INpcManager>();
            if (npcManager == null)
            {
                npcManager = new NpcManager(Current.Game);
            }
            var translationService = RimMindServiceLocator.Get<ITranslationService>();
            var flywheelParameterStore = RimMindServiceLocator.Get<IFlywheelParameterStore>();

            ContextEngine = new ContextOrchestrator(
                HistoryManager,
                npcManager!,
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

            var agentActiveChecker = new AgentActiveChecker();
            RimMindServiceLocator.Register<IAgentActiveChecker>(agentActiveChecker);

#pragma warning disable CS0618
            EventBus = AgentBus;
#pragma warning restore CS0618

            RimMindServiceLocator.Register<IRimMindRuntime>(this);

            var pawnAgentFactory = new PawnAgentFactory(null, AgentBus);
            RimMindServiceLocator.Register<IPawnAgentFactory>(pawnAgentFactory);
            RimMindServiceLocator.Register<IAgentFactory>(pawnAgentFactory);
            RimMindServiceLocator.Register<IGameContextBuilder>(new GameContextBuilder());

            var responseDispatcher = new ResponseDispatcher(AgentBus);
            RimMindServiceLocator.Register<IResponseDispatcher>(responseDispatcher);

            var contextKeyRegistry = new ContextKeyRegistryAdapter();
            RimMindServiceLocator.Register<IContextKeyRegistry>(contextKeyRegistry);

            RimMindServiceLocator.Register(GetExtensionRegistry<ISettingsTab>());
            RimMindServiceLocator.Register(GetExtensionRegistry<IModCooldown>());

            var clientFactoryRegistry = GetExtensionRegistry<IAIClientFactory>();
            clientFactoryRegistry.Register(new OpenAIClientFactory());
            clientFactoryRegistry.Register(new Player2ClientFactory());
            RimMindServiceLocator.Register(clientFactoryRegistry);

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
            Player2Client.StopHealthCheck();
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
                _instance = null;
            }
        }

        public IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension
        {
            return (IExtensionRegistry<T>)_registries.GetOrAdd(typeof(T),
                _ => new ExtensionRegistry<T>());
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

        public void RegisterSensorProvider(ISensorProvider provider)
        {
            _sensorProviders[provider.SensorId] = provider;
            var sensorManager = RimMindServiceLocator.Get<ISensorManager>() as Sensor.SensorManager;
            sensorManager?.RegisterProvider(provider);
        }

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
