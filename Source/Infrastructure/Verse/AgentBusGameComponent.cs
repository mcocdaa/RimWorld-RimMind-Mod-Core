using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Defaults;
using RimMind.Application.Features.Queue;

using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class AgentBusGameComponent : GameComponent
    {
        private IAgentBus? _agentBus;
        private IAgentBus? _subscribersRegisteredBus;
        private readonly List<IDisposable> _coreSubscribers = new();
        private IAIRequestQueueTickable? _requestQueue;
        private AgentBusQueueTickCoordinator? _tickCoordinator;
        private ILogSink? _logSink;
        private IContextCacheManager? _cacheManager;
        private IFlywheelParameterStore? _parameterStore;
        private bool _lifecycleStarted;

        public AgentBusGameComponent(Game game) : base() { }

        // [Framework-Forced SL] Verse GameComponent requires parameterless construction.
        // Reconcile the tick pair because the composition root can replace either service.
        private void EnsureCached()
        {
            IAgentBus? agentBus = RimMindServiceLocator.TryGet<IAgentBus>();
            IAIRequestQueueTickable? requestQueue = RimMindServiceLocator.TryGet<IAIRequestQueueTickable>();
            bool agentBusChanged = !ReferenceEquals(_agentBus, agentBus);

            if (agentBusChanged)
                DisposeCoreSubscribers();

            if (agentBusChanged || !ReferenceEquals(_requestQueue, requestQueue))
            {
                _agentBus = agentBus;
                _requestQueue = requestQueue;
                _tickCoordinator = agentBus != null && requestQueue != null
                    ? new AgentBusQueueTickCoordinator(agentBus, requestQueue)
                    : null;
            }

            if (_requestQueue != null)
                AIRequestQueueGameComponent.Configure(_requestQueue);

            _logSink ??= RimMindServiceLocator.TryGet<ILogSink>();
            _cacheManager ??= RimMindServiceLocator.TryGet<IContextCacheManager>();
            _parameterStore ??= RimMindServiceLocator.TryGet<IFlywheelParameterStore>();

            if (_lifecycleStarted
                && _agentBus != null
                && _logSink != null
                && !ReferenceEquals(_subscribersRegisteredBus, _agentBus))
            {
                ReRegisterCoreSubscribers();
            }
        }

        public override void StartedNewGame()
        {
            EnsureCached();
            ReRegisterCoreSubscribers();
            _lifecycleStarted = true;
        }

        public override void LoadedGame()
        {
            EnsureCached();
            ReRegisterCoreSubscribers();
            _lifecycleStarted = true;
        }

        public override void GameComponentTick()
        {
            EnsureCached();
            _tickCoordinator?.Tick(Find.TickManager.TicksGame);
        }

        private void ReRegisterCoreSubscribers()
        {
            DisposeCoreSubscribers();
            if (_agentBus != null && _logSink != null)
            {
                _coreSubscribers.Add(new AgentBusCoreSubscriber(_agentBus, _logSink));

                if (_cacheManager != null)
                    _coreSubscribers.Add(new ContextInvalidationSubscriber(_agentBus, _cacheManager, _logSink));

                if (_parameterStore != null)
                    _coreSubscribers.Add(new FlywheelCalibrationSubscriber(_agentBus, _parameterStore, _logSink));

                if (_cacheManager != null)
                    _coreSubscribers.Add(new NpcCleanupSubscriber(_agentBus, _cacheManager, _logSink));

                _coreSubscribers.Add(new GoalOptimizationSubscriber(_agentBus, _logSink));

                if (_parameterStore != null)
                    _coreSubscribers.Add(new DecisionTrackingSubscriber(_agentBus, _parameterStore, _logSink));

                _subscribersRegisteredBus = _agentBus;
            }
        }

        private void DisposeCoreSubscribers()
        {
            for (int index = _coreSubscribers.Count - 1; index >= 0; index--)
                _coreSubscribers[index].Dispose();

            _coreSubscribers.Clear();
            _subscribersRegisteredBus = null;
        }
    }
}
