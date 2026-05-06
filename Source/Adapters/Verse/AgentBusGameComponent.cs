using RimMind.Core.Runtime;
using Verse;

namespace RimMind.Adapters.Verse
{
    public class AgentBusGameComponent : GameComponent
    {
        public AgentBusGameComponent(Game game) : base() { }

        public override void StartedNewGame()
        {
            RimMindRuntime.Instance.EventBus.ClearAllSubscribers();
        }

        public override void LoadedGame()
        {
            RimMindRuntime.Instance.EventBus.ClearAllSubscribers();
        }
    }
}
