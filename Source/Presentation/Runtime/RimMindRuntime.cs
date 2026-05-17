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
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
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
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Queue;
using RimMind.Application.Features.Registry;
using RimMind.Application.Features.Tools;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Infrastructure.Services.Clients.OpenAI;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Infrastructure.UI;
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
        public IAgentBus EventBus { get; internal set; }
        public IContextEngine ContextEngine { get; internal set; }
        public IHistoryManager HistoryManager { get; internal set; }
        public IClientManager ClientManager { get; internal set; }
        public IAudioPlayer AudioPlayer { get; internal set; }
        public IProviderRegistry ProviderRegistry { get; internal set; }
        public IOverlayService OverlayService { get; internal set; }
        public AIRequestQueueImpl QueueImpl { get; private set; }
        public IAIRequestQueue Queue => QueueImpl;
        public FlywheelTelemetryCollector Telemetry { get; private set; }
        public ToolRegistry ToolRegistry { get; private set; }
        public GameMechanismRegistry MechanismRegistry { get; private set; }
        public int MaxToolCallDepth { get; set; } = 3;
        private IPipeline<AIRequestContext>? _aiRequestPipeline;
        public IPipeline<AIRequestContext> AIRequestPipeline
        {
            get
            {
                return _aiRequestPipeline ??= AIRequestPipelineFactory.Build(
                    RimMindServiceLocator.Get<ISettingsProvider>()!,
                    GetExtensionRegistry<IMiddleware<AIRequestContext>>());
            }
        }

        private IPipeline<NpcChatContext>? _npcChatPipeline;
        public IPipeline<NpcChatContext> NpcChatPipeline
        {
            get
            {
                return _npcChatPipeline ??= NpcChatPipelineFactory.Build(
                    GetExtensionRegistry<IMiddleware<NpcChatContext>>());
            }
        }

        private IPipeline<ContextBuildContext>? _contextBuildPipeline;
        public IPipeline<ContextBuildContext> ContextBuildPipeline
        {
            get
            {
                return _contextBuildPipeline ??= ContextBuildPipelineFactory.Build(
                    RimMindServiceLocator.Get<ISettingsProvider>()!,
                    GetExtensionRegistry<IMiddleware<ContextBuildContext>>());
            }
        }

        private IPipeline<BusPublishContext>? _busPublishPipeline;
        public IPipeline<BusPublishContext> BusPublishPipeline
        {
            get
            {
                return _busPublishPipeline ??= BusPublishPipelineFactory.Build(
                    AgentBus,
                    RimMindServiceLocator.Get<ILogSink>(),
                    GetExtensionRegistry<IMiddleware<BusPublishContext>>());
            }
        }

        private readonly ConcurrentDictionary<Type, object> _registries = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<string, IParameterTuner> _parameterTuners = new ConcurrentDictionary<string, IParameterTuner>();
        private readonly ConcurrentDictionary<string, ISensorProvider> _sensorProviders = new ConcurrentDictionary<string, ISensorProvider>();

        private volatile Func<Pawn, AgentIdentity?>? _agentIdentityProvider;
        private volatile IAgentActionBridge? _agentActionBridge;
        private volatile bool _isShutdown;

        public IReadOnlyList<IParameterTuner> ParameterTunersList => _parameterTuners.Values.ToList();
        public IReadOnlyList<ISensorProvider> SensorProvidersList => _sensorProviders.Values.ToList();

        public Func<Pawn, AgentIdentity?>? AgentIdentityProvider => _agentIdentityProvider;
        public IAgentActionBridge? AgentActionBridge => _agentActionBridge;
        public bool IsShutdown => _isShutdown;

        private RimMindRuntime()
        {
            Application.DependencyInjection.AddApplicationServices(
                RimMindServiceLocator.Get<ISettingsProvider>());
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
            ProviderRegistry = RimMindServiceLocator.Get<IProviderRegistry>()!;
            HistoryManager = RimMindServiceLocator.Get<IHistoryManager>()!;
            ClientManager = RimMindServiceLocator.Get<IClientManager>()!;
            AudioPlayer = RimMindServiceLocator.Get<IAudioPlayer>()!;
            OverlayService = RimMindServiceLocator.Get<IOverlayService>()!;
            ToolRegistry = RimMindServiceLocator.Get<ToolRegistry>()!;
            MechanismRegistry = RimMindServiceLocator.Get<GameMechanismRegistry>()!;
            Telemetry = RimMindServiceLocator.Get<FlywheelTelemetryCollector>()!;
            QueueImpl = RimMindServiceLocator.Get<AIRequestQueueImpl>()!;

            ContextEngine = new ContextEngine(HistoryManager);
            RimMindServiceLocator.Register<IContextEngine>(ContextEngine);
            RimMindServiceLocator.Register<IContextKeyProvider>(new DefaultContextKeyProvider());

            EventBus = AgentBus;

            RimMindServiceLocator.Register<IRimMindRuntime>(this);

            RimMindServiceLocator.Register<IPawnAgentFactory>(new PawnAgentFactory());
            RimMindServiceLocator.Register<IGameContextBuilder>(new GameContextBuilder());

            var responseDispatcher = new ResponseDispatcher(EventBus);
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
            Telemetry.Dispose();
            ContextEngine.Dispose();
            Player2Client.StopHealthCheck();
        }

        public static void ResetInstance()
        {
            lock (_initLock)
            {
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

        public IAgentActionBridge? GetAgentActionBridge() => _agentActionBridge;

        public void RegisterParameterTuner(IParameterTuner tuner)
            => _parameterTuners[tuner.TunerId] = tuner;

        public void RegisterSensorProvider(ISensorProvider provider)
            => _sensorProviders[provider.SensorId] = provider;

        public void UnregisterSensorProvider(string sensorId)
        {
            _sensorProviders.TryRemove(sensorId, out _);
            ContextKeyRegistry.Unregister($"sensor_{sensorId}");
        }

        public IAIClient? GetClient() => ClientManager.GetClient();
        public void InvalidateClientCache() => ClientManager.InvalidateCache();
        public IAIClient? GetPlayer2Client() => ClientManager.GetPlayer2Client();

        private void RegisterBuiltinModes()
        {
            var modeRegistry = GetExtensionRegistry<IAgentMode>();
            var tickProvider = RimMindServiceLocator.Get<ITickProvider>()
                ?? throw new InvalidOperationException("ITickProvider not registered");
            modeRegistry.Register(new ReactiveAgentMode());
            modeRegistry.Register(new ProactiveAgentMode(tickProvider));
        }

        private void RegisterCoreSubscribers()
        {
            var logSink = RimMindServiceLocator.Get<ILogSink>()!;
            var agentBus = RimMindServiceLocator.Get<IAgentBus>()!;
            new AgentBusCoreSubscriber(agentBus, logSink);
        }
    }

}
