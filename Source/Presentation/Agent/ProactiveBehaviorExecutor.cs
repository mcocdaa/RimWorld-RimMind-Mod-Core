using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Features.Agent;

namespace RimMind.Presentation.Agent
{
    internal sealed class ProactiveBehaviorExecutor
    {
        private readonly IAgentBus _agentBus;
        private readonly IDreamGenerator _dreamGenerator;
        private readonly IDreamThoughtInjector? _dreamThoughtInjector;
        private readonly ITraitEvolver _traitEvolver;
        private readonly ILogSink? _log;

        public ProactiveBehaviorExecutor(
            IAgentBus agentBus,
            IDreamGenerator dreamGenerator,
            IDreamThoughtInjector? dreamThoughtInjector,
            ITraitEvolver traitEvolver,
            ILogSink? log = null)
        {
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
            _dreamGenerator = dreamGenerator ?? throw new ArgumentNullException(nameof(dreamGenerator));
            _dreamThoughtInjector = dreamThoughtInjector;
            _traitEvolver = traitEvolver ?? throw new ArgumentNullException(nameof(traitEvolver));
            _log = log;
        }

        public void ExecuteProactiveExtensions(IPawnAgent agent, IAgentMode mode, int pawnId)
        {
            if (mode is not IProactiveExtensions proactive) return;
            var orchestrator = new ProactiveBehaviorOrchestrator(
                proactive.ReflectionStrategy,
                proactive.DailyPlanner,
                _dreamGenerator,
                _dreamThoughtInjector,
                proactive.TraitEvolutionEngine,
                _traitEvolver,
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
