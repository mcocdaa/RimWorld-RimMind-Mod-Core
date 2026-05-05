using Verse;

namespace RimMind.Core.AgentBus
{
    public class AgentBusGameComponent : GameComponent
    {
        public AgentBusGameComponent(Game game) : base() { }

        public override void StartedNewGame()
        {
            AgentBus.ClearAllSubscribers();
        }

        public override void LoadedGame()
        {
            AgentBus.ClearAllSubscribers();
        }
    }
}
