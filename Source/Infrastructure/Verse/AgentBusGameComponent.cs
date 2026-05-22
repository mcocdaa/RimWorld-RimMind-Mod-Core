using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Defaults;

using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class AgentBusGameComponent : GameComponent
    {
        private IAgentBus? _agentBus;
        private ILogSink? _logSink;
        private IContextCacheManager? _cacheManager;
        private IFlywheelParameterStore? _parameterStore;

        public AgentBusGameComponent(Game game) : base() { }

        // [Framework-Forced SL] Verse GameComponent requires parameterless constructor.
        // EnsureCached() guard pattern: resolves once on first access, then uses cached fields.
        private void EnsureCached()
        {
            if (_agentBus != null) return;
            _agentBus = RimMindServiceLocator.Get<IAgentBus>();
            _logSink = RimMindServiceLocator.Get<ILogSink>();
            _cacheManager = RimMindServiceLocator.Get<IContextCacheManager>();
            _parameterStore = RimMindServiceLocator.Get<IFlywheelParameterStore>();
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
            _agentBus?.FlushBackgroundQueue();
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
