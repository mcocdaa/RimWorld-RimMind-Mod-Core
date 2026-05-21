using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.AgentBus;

using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class AgentBusGameComponent : GameComponent
    {
        private IAgentBus? _agentBus;
        private ILogSink? _logSink;

        public AgentBusGameComponent(Game game) : base() { }

        // [Framework-Forced SL] Verse GameComponent requires parameterless constructor.
        // EnsureCached() guard pattern: resolves once on first access, then uses cached fields.
        private void EnsureCached()
        {
            if (_agentBus != null) return;
            _agentBus = RimMindServiceLocator.Get<IAgentBus>();
            _logSink = RimMindServiceLocator.Get<ILogSink>();
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

        private void ReRegisterCoreSubscribers()
        {
            EnsureCached();
            if (_agentBus != null && _logSink != null)
            {
                _ = new AgentBusCoreSubscriber(_agentBus, _logSink);
            }
        }
    }
}
