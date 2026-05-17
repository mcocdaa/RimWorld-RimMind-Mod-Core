using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnAgentFactory : IPawnAgentFactory
    {
        public object Create(object pawn, object eventBus)
        {
            var tickSettings = RimMindServiceLocator.Get<IAgentTickSettings>();
            var agentBus = RimMindServiceLocator.Get<IAgentBus>();
            return new PawnAgent((Verse.Pawn)pawn, tickSettings!, agentBus!);
        }

        public void SerializeAgent(ref object? agent, string label)
        {
            PawnAgent? concrete = agent as PawnAgent;
            Scribe_Deep.Look(ref concrete, label);
            if (concrete != null)
                agent = concrete;
            else
                agent = null;
        }
    }
}
