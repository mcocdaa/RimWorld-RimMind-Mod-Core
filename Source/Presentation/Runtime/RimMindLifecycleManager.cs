using System;
using System.Collections.Concurrent;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Agent;

namespace RimMind.Presentation.Runtime
{
    /// <summary>
    /// Lifecycle Manager: responsible for Initialize, Shutdown, and Reset operations.
    /// Extracted from RimMindRuntime to satisfy SRP.
    /// </summary>
    internal sealed class RimMindLifecycleManager
    {
        private readonly ITelemetryCollector _telemetry;
        private readonly IContextEngine _contextEngine;
        private readonly IPlayer2Lifecycle? _player2Lifecycle;
        private readonly IAgentBus _agentBus;
        private readonly IContextKeyRegistry? _keyRegistry;
        private volatile bool _isShutdown;

        public bool IsShutdown => _isShutdown;

        public RimMindLifecycleManager(
            ITelemetryCollector telemetry,
            IContextEngine contextEngine,
            IPlayer2Lifecycle? player2Lifecycle,
            IAgentBus agentBus,
            IContextKeyRegistry? keyRegistry = null)
        {
            _telemetry = telemetry;
            _contextEngine = contextEngine;
            _player2Lifecycle = player2Lifecycle;
            _agentBus = agentBus;
            _keyRegistry = keyRegistry;
        }

        public void Shutdown()
        {
            if (_isShutdown) return;
            _isShutdown = true;
            (_telemetry as IDisposable)?.Dispose();
            _contextEngine.Dispose();
            _player2Lifecycle?.StopHealthCheck();
        }

        public void ResetState(
            System.Collections.Concurrent.ConcurrentDictionary<Type, object> registries,
            System.Collections.Concurrent.ConcurrentDictionary<string, IParameterTuner> parameterTuners,
            RimMindExtensionManager extensionManager)
        {
            if (_agentBus is AgentBusImpl busImpl)
                busImpl.ClearAllSubscribers();
            parameterTuners.Clear();
            registries.Clear();
            extensionManager.Reset();
            RimMindServiceLocator.Reset();
            _keyRegistry?.Clear();
            // L3: Static ContextKeyRegistry.ResetCache() and Clear() are no longer needed
            // as the instance-based ContextKeyRegistryImpl.Clear() is called above.
        }
    }
}
