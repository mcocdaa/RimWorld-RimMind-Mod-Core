using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public sealed class AgentsPageDrawer
    {
        private readonly AgentListPanelDrawer _listDrawer = new();
        private readonly AgentDetailPanelDrawer _detailDrawer = new();
        private string _chatDraft = string.Empty;
        private string? _listSelectedPawnId;

        public void Draw(Rect rect, Pawn? hubSelectedPawn, RimMindLayoutScope scope)
        {
            AgentPageRects layout = AgentPageLayout.Calculate(rect);
            scope.Record(layout.List, "Agents:List");
            scope.Record(layout.Detail, "Agents:Detail");

            Pawn? listPawn = _listDrawer.Draw(layout.List, hubSelectedPawn, ref _listSelectedPawnId, scope);
            Pawn? detailPawn = listPawn ?? hubSelectedPawn;
            _detailDrawer.Draw(layout, detailPawn, ref _chatDraft, scope);
        }
    }
}
