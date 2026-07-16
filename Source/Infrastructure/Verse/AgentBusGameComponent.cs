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
        private IAIRequestQueueTickable? _requestQueue;
        private AgentBusQueueTickCoordinator? _tickCoordinator;
        private ILogSink? _logSink;
        private IContextCacheManager? _cacheManager;
        private IFlywheelParameterStore? _parameterStore;

        public AgentBusGameComponent(Game game) : base() { }

        // [Framework-Forced SL] Verse GameComponent requires parameterless construction.
        // Reconcile the tick pair because the composition root can replace either service.
        private void EnsureCached()
        {
            IAgentBus? agentBus = RimMindServiceLocator.TryGet<IAgentBus>();
            IAIRequestQueueTickable? requestQueue = RimMindServiceLocator.TryGet<IAIRequestQueueTickable>();

            if (!ReferenceEquals(_agentBus, agentBus) || !ReferenceEquals(_requestQueue, requestQueue))
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
        }

        public override void StartedNewGame()
        {
            EnsureCached();
            _agentBus?.ClearAllSubscribers();
            ReRegisterCoreSubscribers();
        }

        public override void LoadedGame()
        {
            EnsureCached();
            _agentBus?.ClearAllSubscribers();
            ReRegisterCoreSubscribers();
        }

        public override void GameComponentTick()
        {
            EnsureCached();
            _tickCoordinator?.Tick(Find.TickManager.TicksGame);
        }

        private void ReRegisterCoreSubscribers()
        {
            EnsureCached();
            if (_agentBus != null && _logSink != null)
            {
                _ = new AgentBusCoreSubscriber(_agentBus, _logSink);

                if (_cacheManager != null)
                    _ = new ContextInvalidationSubscriber(_agentBus, _cacheManager, _logSink);

                if (_parameterStore != null)
                    _ = new FlywheelCalibrationSubscriber(_agentBus, _parameterStore, _logSink);

                if (_cacheManager != null)
                    _ = new NpcCleanupSubscriber(_agentBus, _cacheManager, _logSink);

                _ = new GoalOptimizationSubscriber(_agentBus, _logSink);

                if (_parameterStore != null)
                    _ = new DecisionTrackingSubscriber(_agentBus, _parameterStore, _logSink);
            }
        }
    }
}
