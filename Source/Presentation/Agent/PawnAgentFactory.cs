using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnAgentFactory : IPawnAgentFactory
    {
        private readonly IAgentTickSettings? _tickSettings;
        private readonly IAgentBus _agentBus;

        internal IAgentTickSettings? TickSettings => _tickSettings;
        internal IAgentBus AgentBus => _agentBus;

        public PawnAgentFactory(IAgentTickSettings? tickSettings, IAgentBus agentBus)
        {
            _tickSettings = tickSettings;
            _agentBus = agentBus;
        }

        public IPawnAgent Create(Pawn pawn, IAgentBus agentBus)
        {
            var agent = new PawnAgent(pawn, _tickSettings!, agentBus);
            agent.RebuildCollaborators(
                new PawnPerceiver(agent, agentBus),
                new PawnThinker(agent, _tickSettings!, agentBus),
                new PawnActor(agent),
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
