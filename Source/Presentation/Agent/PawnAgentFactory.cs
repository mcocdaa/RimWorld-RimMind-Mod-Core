using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnAgentFactory : IPawnAgentFactory
    {
        private readonly IAgentTickSettings? _tickSettings;
        private readonly IAgentBus _agentBus;

        public PawnAgentFactory(IAgentTickSettings? tickSettings, IAgentBus agentBus)
        {
            _tickSettings = tickSettings;
            _agentBus = agentBus;
        }

        public IPawnAgent Create(Pawn pawn, IAgentBus agentBus)
        {
            return new PawnAgent(pawn, _tickSettings!, agentBus);
        }

        public void SerializeAgent(ref IPawnAgent? agent, string label)
        {
            PawnAgent? concrete = agent as PawnAgent;
            Scribe_Deep.Look(ref concrete, label);
            agent = concrete;
        }
    }
}
