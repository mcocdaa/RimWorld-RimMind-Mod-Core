using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnAgentFactory : IPawnAgentFactory
    {
        private readonly IAgentTickSettings? _tickSettings;
        private readonly IAgentBus _agentBus;
        private readonly IActionExecutor _actionExecutor;
        private readonly ILogSink? _log;

        internal IAgentTickSettings? TickSettings => _tickSettings;
        internal IAgentBus AgentBus => _agentBus;
        internal IActionExecutor ActionExecutor => _actionExecutor;
        internal ILogSink? LogSink => _log;

        public PawnAgentFactory(IAgentTickSettings? tickSettings, IAgentBus agentBus, IActionExecutor actionExecutor, ILogSink? log = null)
        {
            _tickSettings = tickSettings;
            _agentBus = agentBus;
            _actionExecutor = actionExecutor;
            _log = log;
        }

        public IPawnAgent Create(Pawn pawn, IAgentBus agentBus)
        {
            var agent = new PawnAgent(pawn, _tickSettings!, agentBus, log: _log);
            agent.RebuildCollaborators(
                new PawnPerceiver(agent, agentBus),
                new PawnThinker(agent, _tickSettings!, agentBus, _log),
                new PawnActor(agent, _actionExecutor),
                new PawnRecorder(agent, agentBus));
            return agent;
        }

        public void SerializeAgent(ref IPawnAgent? agent, string label)
        {
            // Verse Scribe_Deep.Look requires concrete PawnAgent type, not interface.
            // Encapsulate the type conversion within PawnAgent.Serialize/Deserialize.
            PawnAgent.Serialize(ref agent, label, this);
        }
    }
}
