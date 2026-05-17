using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class AgentBusGameComponent : GameComponent
    {
        public AgentBusGameComponent(Game game) : base() { }

        public override void StartedNewGame()
        {
            RimMindServiceLocator.Get<IAgentBus>()?.ClearAllSubscribers();
        }

        public override void LoadedGame()
        {
            RimMindServiceLocator.Get<IAgentBus>()?.ClearAllSubscribers();
        }
    }
}
