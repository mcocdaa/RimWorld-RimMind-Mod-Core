using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;

namespace RimMind.Presentation.Agent
{
    public class PawnAgentFactory : IPawnAgentFactory
    {
        public object Create(object pawn, object eventBus)
        {
            return new PawnAgent((Verse.Pawn)pawn, (IEventBus)eventBus);
        }
    }
}
