using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnAgentFactory : IPawnAgentFactory, IAgentFactory
    {
        private readonly IAgentTickSettings? _tickSettings;
        private readonly IAgentBus _agentBus;

        public PawnAgentFactory(IAgentTickSettings? tickSettings = null, IAgentBus? agentBus = null)
        {
            _tickSettings = tickSettings;
            _agentBus = agentBus ?? RimMindServiceLocator.Get<IAgentBus>()!;
        }

        public IPawnAgent Create(Pawn pawn, IAgentBus agentBus)
        {
            var tickSettings = _tickSettings ?? RimMindServiceLocator.Get<IAgentTickSettings>();
            return new PawnAgent(pawn, tickSettings!, agentBus);
        }

        public void SerializeAgent(ref IPawnAgent? agent, string label)
        {
            PawnAgent? concrete = agent as PawnAgent;
            Scribe_Deep.Look(ref concrete, label);
            agent = concrete;
        }

        IAgentControl IAgentFactory.CreateAgent(object pawn, IAgentBus agentBus)
        {
            return Create((Pawn)pawn, agentBus);
        }

        void IAgentFactory.SerializeAgent(ref IAgentControl? agent, string label)
        {
            IPawnAgent? pawnAgent = agent as IPawnAgent;
            SerializeAgent(ref pawnAgent, label);
            agent = pawnAgent;
        }
    }
}
