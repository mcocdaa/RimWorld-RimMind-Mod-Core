using Verse;

namespace RimMind.Core.Runtime
{
    public sealed class RimMindRuntimeGameComponent : GameComponent
    {
        public RimMindRuntimeGameComponent(Game game) { }

        public override void StartedNewGame()
        {
            RimMindRuntime.Instance.Reset();
        }

        public override void LoadedGame()
        {
            RimMindRuntime.Instance.Reset();
        }
    }
}
