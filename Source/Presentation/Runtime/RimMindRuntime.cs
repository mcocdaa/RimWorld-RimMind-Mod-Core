using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Runtime;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Queue;
using RimMind.Application.Features.Registry;
using RimMind.Application.Features.Tools;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Infrastructure.UI;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Context;
using RimMind.Presentation.Pipeline.AI;
using RimMind.Presentation.Pipeline.Context;
using RimMind.Presentation.Pipeline.Npc;
using RimMind.Presentation.Settings;
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
        public IEventBus EventBus { get; internal set; }
        public IContextEngine ContextEngine { get; internal set; }
        public IHistoryManager HistoryManager { get; internal set; }
        public IClientManager ClientManager { get; internal set; }
        public IAudioPlayer AudioPlayer { get; internal set; }
        public IProviderRegistry ProviderRegistry { get; internal set; }
        public IOverlayService OverlayService { get; internal set; }
        public AIRequestQueueImpl QueueImpl { get; private set; }
        public IAIRequestQueue Queue => QueueImpl;
        public FlywheelTelemetryCollector Telemetry { get; private set; }
        public ToolRegistry ToolRegistry { get; private set; } = new();
        public GameMechanismRegistry MechanismRegistry { get; private set; }
        public int MaxToolCallDepth { get; set; } = 3;
        public IPipeline<AIRequestContext> AIRequestPipeline { get; private set; }
        public IPipeline<NpcChatContext> NpcChatPipeline { get; private set; }
        public IPipeline<ContextBuildContext> ContextBuildPipeline { get; private set; }

        private readonly ConcurrentDictionary<Type, object> _registries = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<string, IParameterTuner> _parameterTuners = new ConcurrentDictionary<string, IParameterTuner>();
        private readonly ConcurrentDictionary<string, IKernelParameterTuner> _kernelParameterTuners = new ConcurrentDictionary<string, IKernelParameterTuner>();
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
            ProviderRegistry = new ProviderRegistry();
            ClientManager = new ClientManager();
            OverlayService = new OverlayService();
            HistoryManager = new HistoryManager();
            ContextEngine = new ContextEngine(HistoryManager);
            AgentBus = new AgentBusImpl();
            EventBus = new SimpleEventBusAdapter(AgentBus);
            AudioPlayer = new NullAudioPlayer();
            Telemetry = new FlywheelTelemetryCollector();
            QueueImpl = new AIRequestQueueImpl();
            MechanismRegistry = new GameMechanismRegistry(ToolRegistry);

            AIRequestPipeline = AIRequestPipelineFactory.Build(
                ToolRegistry,
                GetExtensionRegistry<IMiddleware<AIRequestContext>>(),
                () => MaxToolCallDepth,
                AgentBus);
            NpcChatPipeline = NpcChatPipelineFactory.Build(
                GetExtensionRegistry<IMiddleware<NpcChatContext>>());
            ContextBuildPipeline = ContextBuildPipelineFactory.Build(
                (ContextOrchestrator)((ContextEngine)ContextEngine).Orchestrator!,
                GetExtensionRegistry<IMiddleware<ContextBuildContext>>());
        }

        public static void Initialize()
        {
            lock (_initLock)
            {
                if (_instance != null) return;
                _instance = new RimMindRuntime();
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
            => _agentActionBridge = bridge;

        public IAgentActionBridge? GetAgentActionBridge() => _agentActionBridge;

        public void RegisterParameterTuner(IParameterTuner tuner)
            => _parameterTuners[tuner.TunerId] = tuner;

        void IRimMindRuntime.RegisterParameterTuner(IKernelParameterTuner tuner)
            => _kernelParameterTuners[tuner.TunerId] = tuner;

        IReadOnlyList<IKernelParameterTuner> IRimMindRuntime.ParameterTunersList
            => _kernelParameterTuners.Values.ToList();

        public void RegisterSensorProvider(ISensorProvider provider)
            => _sensorProviders[provider.SensorId] = provider;

        public void UnregisterSensorProvider(string sensorId)
        {
            _sensorProviders.TryRemove(sensorId, out _);
            ContextKeyRegistry.Unregister($"sensor_{sensorId}");
        }

        public IAIClient? GetClient() => ClientManager.GetClient();
        public void InvalidateClientCache() => ClientManager.InvalidateCache();
        public Player2Client? GetPlayer2Client() => ClientManager.GetPlayer2Client() as Player2Client;
    }

    internal sealed class SimpleEventBusAdapter : IEventBus
    {
        private readonly IAgentBus _bus;
        public SimpleEventBusAdapter(IAgentBus bus) => _bus = bus;
        public void Subscribe<T>(string key, Action<T> handler) where T : Domain.Events.AgentBusEvent => _bus.Subscribe(key, handler);
        public string Subscribe<T>(Action<T> handler) where T : Domain.Events.AgentBusEvent => _bus.Subscribe(handler);
        public void Unsubscribe<T>(string key) where T : Domain.Events.AgentBusEvent => _bus.Unsubscribe<T>(key);
        public void Unsubscribe<T>(Action<T> handler) where T : Domain.Events.AgentBusEvent => _bus.Unsubscribe(handler);
        public void Publish<T>(T evt) where T : Domain.Events.AgentBusEvent => _bus.Publish(evt);
        public void PublishFromBackground<T>(T evt) where T : Domain.Events.AgentBusEvent => _bus.PublishFromBackground(evt);
        public void FlushBackgroundQueue() => _bus.FlushBackgroundQueue();
        public void ClearAllSubscribers() => _bus.ClearAllSubscribers();
        public int GetHandlerCount() => 0;
        public int GetBackgroundQueueCount() => 0;
    }
}
