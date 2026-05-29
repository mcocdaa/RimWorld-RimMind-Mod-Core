using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Agent;

namespace RimMind.Presentation.Agent
{
    internal sealed class ProactiveBehaviorExecutor
    {
        private readonly IAgentBus _agentBus;
        private readonly ILogSink? _log;

        public ProactiveBehaviorExecutor(IAgentBus agentBus, ILogSink? log = null)
        {
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
            _log = log;
        }

        public void ExecuteProactiveExtensions(IPawnAgent agent, IAgentMode mode, int pawnId)
        {
            if (mode is not IProactiveExtensions proactive) return;
            var orchestrator = new ProactiveBehaviorOrchestrator(
                proactive.ReflectionStrategy,
                proactive.DailyPlanner,
                RimMindServiceLocator.Get<IDreamGenerator>(),
                RimMindServiceLocator.Get<IDreamThoughtInjector>(),
                proactive.TraitEvolutionEngine,
                RimMindServiceLocator.Get<ITraitEvolver>(),
                _agentBus,
                pawnId,
                _log);
            orchestrator.ExecuteReflection(agent);
            orchestrator.ExecutePlanning(agent);
            orchestrator.ExecuteDream(agent);
            orchestrator.ExecuteTraitEvolution(agent);
        }
    }
}
