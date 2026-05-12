using RimMind.Contracts.Npc;
using RimMind.Core.Sensor;
using RimMind.Contracts.Sensor;
using RimMind.Contracts.Tools;
using RimMind.Contracts.Mechanisms;
using RimMind.Kernel.Tools;
using RimMind.Kernel.Mechanisms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.AI;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Kernel.Pipeline.Context;
using RimMind.Kernel.Pipeline.Bus;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using RimMind.Contracts.Client;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Registry;
using RimMind.Kernel.Logging;
using RimMind.Adapters.UI;
using RimMind.Contracts.UI;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Pipeline;
using RimMind.Kernel.Prompt;
using RimMind.Kernel.Queue;
using RimMind.Adapters.Client.Player2;
using RimMind.Kernel.Registry;
using RimMind.Contracts.Result;
using Verse;

using RimMind.Contracts.Runtime;
using RimMind.Contracts.Flywheel;

namespace RimMind.Core.Runtime
{
    internal sealed class RimMindRuntime : IRimMindRuntime
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

        private readonly ConcurrentDictionary<Type, object> _busPipelines
            = new ConcurrentDictionary<Type, object>();

        private readonly ConcurrentDictionary<Type, object> _registries
            = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<string, IParameterTuner> _parameterTuners
            = new ConcurrentDictionary<string, IParameterTuner>();
        private readonly ConcurrentDictionary<string, IKernelParameterTuner> _kernelParameterTuners
            = new ConcurrentDictionary<string, IKernelParameterTuner>();
        private readonly ConcurrentDictionary<string, ISensorProvider> _sensorProviders
            = new ConcurrentDictionary<string, ISensorProvider>();

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
                ((ContextEngine)ContextEngine).Orchestrator,
                ((ContextEngine)ContextEngine).CacheManager,
                GetExtensionRegistry<IMiddleware<ContextBuildContext>>());
            ((ContextEngine)ContextEngine).PipelineBuildSnapshot = req =>
            {
                var ctx = new ContextBuildContext { Request = req };
                ContextBuildPipeline.ExecuteAsync(ctx).GetAwaiter().GetResult();
                return ctx.Snapshot;
            };
            ((AgentBusImpl)AgentBus).SetPublishViaPipeline((evt, subscribers, isBackground) =>
            {
                var eventType = evt.GetType();
                var method = typeof(RimMindRuntime).GetMethod(
                    nameof(PublishEventViaPipeline),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var generic = method.MakeGenericMethod(eventType);
                return (bool)generic.Invoke(this, new object[] { evt, subscribers, isBackground });
            });
            QueueImpl.SetExecuteViaPipeline((req, client) =>
            {
                var ctx = new AIRequestContext { Request = req, Client = client };
                req.TraceId = ctx.TraceId;
                AIRequestPipeline.ExecuteAsync(ctx).GetAwaiter().GetResult();
                return ctx.Result?.Match(ok => ok, err => AIResponse.Ok(req.RequestId, "", 0)) ?? AIResponse.Ok(req.RequestId, "", 0);
            });

            RimMindServiceLocator.Register<IHistoryManager>(HistoryManager);
            RimMindServiceLocator.Register<IRimMindRuntime>(this);

            RegisterAllMechanisms();
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

        public void Reset()
        {
            RimMindServiceLocator.Reset();

            HistoryManager = new HistoryManager();
            RimMindServiceLocator.Register<IHistoryManager>(HistoryManager);
            ContextEngine = new ContextEngine(HistoryManager);
            ProviderRegistry.Reset();
            _agentIdentityProvider = null;
            _agentActionBridge = null;
            AudioPlayer = new NullAudioPlayer();
            Telemetry = new FlywheelTelemetryCollector();
            _parameterTuners.Clear();
            _sensorProviders.Clear();
            _registries.Clear();
            ToolRegistry = new ToolRegistry();
            MechanismRegistry = new GameMechanismRegistry(ToolRegistry);
            MaxToolCallDepth = 3;
            AgentBus = new AgentBusImpl();
            EventBus = new SimpleEventBusAdapter(AgentBus);
            ClientManager = new ClientManager();
            QueueImpl = new AIRequestQueueImpl();
            AIRequestPipeline = AIRequestPipelineFactory.Build(
                ToolRegistry,
                GetExtensionRegistry<IMiddleware<AIRequestContext>>(),
                () => MaxToolCallDepth,
                AgentBus);
            NpcChatPipeline = NpcChatPipelineFactory.Build(
                GetExtensionRegistry<IMiddleware<NpcChatContext>>());
            ContextBuildPipeline = ContextBuildPipelineFactory.Build(
                ((ContextEngine)ContextEngine).Orchestrator,
                ((ContextEngine)ContextEngine).CacheManager,
                GetExtensionRegistry<IMiddleware<ContextBuildContext>>());
            ((ContextEngine)ContextEngine).PipelineBuildSnapshot = req =>
            {
                var ctx = new ContextBuildContext { Request = req };
                ContextBuildPipeline.ExecuteAsync(ctx).GetAwaiter().GetResult();
                return ctx.Snapshot;
            };
            ((AgentBusImpl)AgentBus).SetPublishViaPipeline((evt, subscribers, isBackground) =>
            {
                var eventType = evt.GetType();
                var method = typeof(RimMindRuntime).GetMethod(
                    nameof(PublishEventViaPipeline),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var generic = method.MakeGenericMethod(eventType);
                return (bool)generic.Invoke(this, new object[] { evt, subscribers, isBackground });
            });
            QueueImpl.SetExecuteViaPipeline((req, client) =>
            {
                var ctx = new AIRequestContext { Request = req, Client = client };
                req.TraceId = ctx.TraceId;
                AIRequestPipeline.ExecuteAsync(ctx).GetAwaiter().GetResult();
                return ctx.Result?.Match(ok => ok, err => AIResponse.Ok(req.RequestId, "", 0)) ?? AIResponse.Ok(req.RequestId, "", 0);
            });
            _busPipelines.Clear();
            _isShutdown = false;

            RegisterAllMechanisms();
        }

        private void RegisterAllMechanisms()
        {
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Skill.SkillMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Need.NeedMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Map.Wealth.WealthMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Job.JobMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Draft.DraftMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Work.WorkMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Equipment.EquipmentMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Interaction.InteractionMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Recruit.RecruitMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Thought.ThoughtMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Inspiration.InspirationMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.MentalState.MentalStateMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Health.HealthMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.Pawn.Relations.RelationsMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.World.Faction.FactionMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.World.Storyteller.StorytellerMechanism());
            MechanismRegistry.Register(new Kernel.Mechanisms.World.ChoiceLetter.ChoiceLetterMechanism());
        }

        public void Shutdown()
        {
            if (_isShutdown) return;
            _isShutdown = true;
            Telemetry.Dispose();
            ContextEngine.Dispose();
            Player2Client.StopHealthCheck();
        }

        public IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension
        {
            return (IExtensionRegistry<T>)_registries.GetOrAdd(typeof(T),
                _ => new ExtensionRegistry<T>());
        }

        private bool PublishEventViaPipeline<T>(T evt, Delegate[] subscribers, bool isBackground) where T : Contracts.AgentBusEvent
        {
            var pipeline = GetOrCreateBusPublishPipeline<T>();
            var ctx = new BusPublishContext<T>
            {
                Event = evt,
                IsBackground = isBackground,
                Subscribers = subscribers
            };
            pipeline.ExecuteAsync(ctx).GetAwaiter().GetResult();
            return true;
        }

        public void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider)
            => _agentIdentityProvider = provider;

        public AgentIdentity? GetAgentIdentity(Pawn pawn)
            => _agentIdentityProvider?.Invoke(pawn);

        public void RegisterAgentActionBridge(IAgentActionBridge bridge)
            => _agentActionBridge = bridge;

        public IAgentActionBridge? GetAgentActionBridge()
            => _agentActionBridge;

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
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => ContextEngine.GetEmbeddingSnapshotStore();

        public IPipeline<BusPublishContext<T>> GetOrCreateBusPublishPipeline<T>()
            where T : RimMind.Contracts.AgentBusEvent
        {
            return (IPipeline<BusPublishContext<T>>)_busPipelines.GetOrAdd(typeof(T),
                _ => BusPublishPipelineFactory<T>.Build(
                    GetExtensionRegistry<IMiddleware<BusPublishContext<T>>>()));
        }

        public void RequestStructuredAsync(AIRequest request, string? jsonSchema, Action<AIResponse> onComplete, List<StructuredTool>? tools = null)
        {
            var s = RimMindCoreMod.Settings;
            if (s == null || !s.IsConfigured())
            {
                onComplete?.Invoke(AIResponse.Ok(request.RequestId, "", 0));
                return;
            }
            request.UseJsonMode = true;
            if (!string.IsNullOrEmpty(jsonSchema))
                request.JsonSchema = jsonSchema;
            if (tools != null && tools.Count > 0)
                request.Tools = tools;
            var client = GetClient();
            if (client == null)
            {
                onComplete?.Invoke(AIResponse.Ok(request.RequestId, "", 0));
                return;
            }
            Queue.Enqueue(request, onComplete, client);
        }

        public IDisposable WithOverrides(Action<RuntimeOverrides> configure)
        {
            var snapshot = CaptureSnapshot();
            var overrides = new RuntimeOverrides(this);
            configure(overrides);
            overrides.Apply();
            return new RuntimeScope(this, snapshot);
        }

        private RuntimeSnapshot CaptureSnapshot()
        {
            return new RuntimeSnapshot
            {
                AgentBus = AgentBus,
                EventBus = EventBus,
                ContextEngine = ContextEngine,
                HistoryManager = HistoryManager,
                ClientManager = ClientManager,
                AudioPlayer = AudioPlayer,
            };
        }

        private void RestoreSnapshot(RuntimeSnapshot snapshot)
        {
            AgentBus = snapshot.AgentBus;
            EventBus = snapshot.EventBus;
            ContextEngine = snapshot.ContextEngine;
            HistoryManager = snapshot.HistoryManager;
            ClientManager = snapshot.ClientManager;
            AudioPlayer = snapshot.AudioPlayer;
        }

        private sealed class RuntimeScope : IDisposable
        {
            private readonly RimMindRuntime _runtime;
            private readonly RuntimeSnapshot _snapshot;
            private bool _disposed;

            public RuntimeScope(RimMindRuntime runtime, RuntimeSnapshot snapshot)
            {
                _runtime = runtime;
                _snapshot = snapshot;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _runtime.RestoreSnapshot(_snapshot);
            }
        }

        private sealed class RuntimeSnapshot
        {
            public IAgentBus AgentBus = null!;
            public IEventBus EventBus = null!;
            public IContextEngine ContextEngine = null!;
            public IHistoryManager HistoryManager = null!;
            public IClientManager ClientManager = null!;
            public IAudioPlayer AudioPlayer = null!;
        }
    }

    internal sealed class SimpleEventBusAdapter : IEventBus
    {
        private readonly IAgentBus _bus;
        public SimpleEventBusAdapter(IAgentBus bus) => _bus = bus;
        public void Subscribe<T>(string key, Action<T> handler) where T : Contracts.AgentBusEvent => _bus.Subscribe(key, handler);
        public string Subscribe<T>(Action<T> handler) where T : Contracts.AgentBusEvent => _bus.Subscribe(handler);
        public void Unsubscribe<T>(string key) where T : Contracts.AgentBusEvent => _bus.Unsubscribe<T>(key);
        public void Unsubscribe<T>(Action<T> handler) where T : Contracts.AgentBusEvent => _bus.Unsubscribe(handler);
        public void Publish<T>(T evt) where T : Contracts.AgentBusEvent => _bus.Publish(evt);
        public void PublishFromBackground<T>(T evt) where T : Contracts.AgentBusEvent => _bus.PublishFromBackground(evt);
        public void FlushBackgroundQueue() => _bus.FlushBackgroundQueue();
        public void ClearAllSubscribers() => _bus.ClearAllSubscribers();
        public int GetHandlerCount() => 0;
        public int GetBackgroundQueueCount() => 0;
    }
}
