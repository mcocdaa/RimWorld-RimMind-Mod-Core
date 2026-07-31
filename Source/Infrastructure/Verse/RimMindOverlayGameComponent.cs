using RimMind.Infrastructure.UI;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class RimMindOverlayGameComponent : GameComponent
    {
        public RimMindOverlayGameComponent(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing) return;

            RequestOverlay.OnGUI();
        }
    }
}
