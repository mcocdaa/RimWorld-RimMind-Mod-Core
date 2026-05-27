using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
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
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Registry;
using Verse;

namespace RimMind.Presentation.Runtime
{
    /// <summary>
    /// Thin facade for the RimMind runtime. Delegates to:
    /// - RimMindCompositionRoot: service instantiation and DI registration
    /// - RimMindLifecycleManager: Initialize/Shutdown lifecycle
    /// - RimMindExtensionManager: extension registration, middleware, agent identity
    /// </summary>
    public sealed class RimMindRuntime : IRimMindRuntime
    {
        private static RimMindRuntime? _instance;
        private static readonly object _initLock = new object();

        public static RimMindRuntime Instance => _instance
            ?? throw new InvalidOperationException("[RimMind-Core] RimMindRuntime not initialized. Call Initialize() first.");

        // Sub-managers (SRP decomposition)
        private readonly RimMindCompositionRoot.CompositionResult _composition;
        private readonly RimMindLifecycleManager _lifecycleManager;
        private readonly RimMindExtensionManager _extensionManager;

        // Extension state (kept here for backward compatibility with public API)
        private readonly ConcurrentDictionary<Type, object> _registries = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<string, IParameterTuner> _parameterTuners = new ConcurrentDictionary<string, IParameterTuner>();

        // Public properties — delegate to CompositionResult
        public IAgentBus AgentBus => _composition.AgentBus;
        public IContextEngine ContextEngine => _composition.ContextEngine;
        public IHistoryManager HistoryManager => _composition.HistoryManager;
        public IClientManager ClientManager => _composition.ClientManager;
        public IAudioPlayer AudioPlayer => _composition.AudioPlayer;
        public IProviderRegistry ProviderRegistry => _composition.ProviderRegistry;
        public IOverlayService OverlayService => _composition.OverlayService;
        public IAIRequestQueue Queue => _composition.Queue;
        public ITelemetryCollector Telemetry => _composition.Telemetry;
        public IToolRegistry ToolRegistry => _composition.ToolRegistry;
        public IGameMechanismRegistry MechanismRegistry => _composition.MechanismRegistry;
        public IWindowService? WindowService => _composition.WindowService;
        public int MaxToolCallDepth { get; set; } = 3;
        public IContextKeyRegistry ContextKeys => _composition.ContextKeyRegistry;
        public IRelevanceTable RelevanceTable => _composition.RelevanceTable;
        public IRelevanceLearner ContextLearner => _composition.RelevanceLearner;

        public IPipeline<BusPublishContext> BusPublishPipeline => _composition.BusPublishPipeline;
        public IPipeline<LlmRequestContext> UnifiedPipeline => _composition.UnifiedPipeline;

        public IReadOnlyList<IParameterTuner> ParameterTunersList => _parameterTuners.Values.ToList();
        public Func<Pawn, AgentIdentity?>? AgentIdentityProvider => _extensionManager.AgentIdentityProvider;
        public IAgentActionBridge AgentActionBridge => _extensionManager.AgentActionBridge;
        public bool IsShutdown => _lifecycleManager.IsShutdown;

        private RimMindRuntime(ISettingsProvider? settingsProvider, IOpenAISettings? openAISettings)
        {
            // Step 1: Compose all services (Composition Root)
            var compositionRoot = new RimMindCompositionRoot();
            _composition = compositionRoot.Compose(settingsProvider, openAISettings);

            // Step 2: Create Lifecycle Manager
            _lifecycleManager = new RimMindLifecycleManager(
                _composition.Telemetry,
                _composition.ContextEngine,
                _composition.Player2Lifecycle,
                _composition.AgentBus,
                _composition.ContextKeyRegistry);

            // Step 3: Create Extension Manager
            _extensionManager = new RimMindExtensionManager(
                _composition.LogSink,
                _composition.TickProvider,
                _composition.AgentBus);

            // Step 4: Register runtime itself
            RimMindServiceLocator.Register<IRimMindRuntime>(this);
        }

        public static void Initialize(ISettingsProvider? settingsProvider = null, IOpenAISettings? openAISettings = null)
        {
            lock (_initLock)
            {
                if (_instance != null) return;
                _instance = new RimMindRuntime(settingsProvider, openAISettings);
                _instance._extensionManager.RegisterBuiltinModes(_instance.GetExtensionRegistry<IAgentMode>());
                Log.Message("[RimMind-Core] Runtime initialized");
            }
        }

        public void Shutdown() => _lifecycleManager.Shutdown();

        public static void ResetInstance()
        {
            lock (_initLock)
            {
                if (_instance != null)
                {
                    _instance._lifecycleManager.Shutdown();
                    _instance._lifecycleManager.ResetState(
                        _instance._registries,
                        _instance._parameterTuners,
                        _instance._extensionManager);
                }
                _instance = null;
            }
        }

        public IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension
        {
            // Delegate to ServiceLocator to ensure single source of truth.
            // Previously used _registries dict which created separate instances from CompositionRoot's SL,
            // causing sub-Mod extensions to be invisible to Pipeline factories.
            var existing = RimMindServiceLocator.Get<IExtensionRegistry<T>>();
            if (existing != null) return existing;
            var newRegistry = new ExtensionRegistry<T>();
            RimMindServiceLocator.Register(newRegistry);
            return newRegistry;
        }

        public void AddMiddleware<TContext>(IMiddleware<TContext> middleware) where TContext : IPipelineContext
        {
            _extensionManager.AddMiddleware(
                middleware,
                _composition.BusPublishPipeline);
        }

        public void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider)
            => _extensionManager.RegisterAgentIdentityProvider(provider);

        public AgentIdentity? GetAgentIdentity(Pawn pawn)
            => _extensionManager.GetAgentIdentity(pawn);

        public void RegisterAgentActionBridge(IAgentActionBridge bridge)
            => _extensionManager.RegisterAgentActionBridge(bridge);

        public IAgentActionBridge GetAgentActionBridge() => _extensionManager.GetAgentActionBridge();

        public void RegisterParameterTuner(IParameterTuner tuner)
            => _extensionManager.RegisterParameterTuner(tuner, _parameterTuners);

        public IAIClient? GetClient() => ClientManager.GetClient();
        public void InvalidateClientCache() => ClientManager.InvalidateCache();
        public IAIClient? GetPlayer2Client() => ClientManager.GetPlayer2Client();
        public ISettingsProvider? GetSettingsProvider() => _composition.SettingsProvider;

        public T? GetService<T>() where T : class => RimMindServiceLocator.Get<T>();
        public void RegisterService<T>(T instance) where T : class => RimMindServiceLocator.Register<T>(instance);
    }
}
