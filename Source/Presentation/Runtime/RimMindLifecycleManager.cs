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
using RimMind.Presentation.Context;

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
        private volatile bool _isShutdown;

        public bool IsShutdown => _isShutdown;

        public RimMindLifecycleManager(
            ITelemetryCollector telemetry,
            IContextEngine contextEngine,
            IPlayer2Lifecycle? player2Lifecycle,
            IAgentBus agentBus)
        {
            _telemetry = telemetry;
            _contextEngine = contextEngine;
            _player2Lifecycle = player2Lifecycle;
            _agentBus = agentBus;
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
            ContextKeyRegistry.ResetCache();
            ContextKeyRegistry.Clear();
            GameContextBuilder.ResetCache();
        }
    }
}
