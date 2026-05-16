using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnAgentFactory : IPawnAgentFactory
    {
        public object Create(object pawn, object eventBus)
        {
            return new PawnAgent((Verse.Pawn)pawn, (IEventBus)eventBus);
        }

        public void SerializeAgent(ref IPawnAgent? agent, string label)
        {
            PawnAgent? concrete = agent as PawnAgent;
            Scribe_Deep.Look(ref concrete, label);
            if (concrete != null)
                agent = concrete;
        }
    }
}
