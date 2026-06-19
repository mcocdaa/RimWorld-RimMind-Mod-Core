using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter
{
    public sealed class DebugCenterPageContext
    {
        public DebugCenterPageContext(Pawn? selectedPawn)
        {
            SelectedPawn = selectedPawn;
        }

        public Pawn? SelectedPawn { get; }
    }
}
