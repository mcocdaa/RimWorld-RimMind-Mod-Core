using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.UI;
using RimMind.Infrastructure.Verse;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class OverviewDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "overview",
            "RimMind.UI.Hub.Tab.Overview",
            0,
            IsDefault: false);

        public void Draw(Rect rect, DebugCenterPageContext context)
        {
            Pawn? selectedPawn = context.SelectedPawn
                ?? Find.Selector.SingleSelectedThing as Pawn;
            int agentCount = 0;
            int activeCount = 0;
            var map = Find.CurrentMap;

            if (map != null)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var agent = CompPawnAgent.GetComp(pawn)?.Agent;
                    if (agent == null) continue;
                    agentCount++;
                    if (agent.IsActive)
                        activeCount++;
                }
            }

            var queue = RimMindServiceLocator.TryGet<IAIRequestQueue>();
            string queueText = queue == null
                ? "RimMind.UI.Hub.QueueMissing".Translate()
                : queue.IsPaused
                    ? "RimMind.Settings.QueuePaused".Translate()
                    : "RimMind.Settings.QueueRunning".Translate();

            float y = rect.y;
            float cardW = (rect.width - RimMindUI.Padding) / 2f;
            const float cardH = 60f;

            Rect agentCard = new Rect(rect.x, y, cardW, cardH);
            Widgets.DrawBoxSolid(agentCard, RimMindUI.ColorCardBg);
            float innerY = agentCard.y + RimMindUI.Padding;
            GUI.color = RimMindUI.ColorSectionTitle;
            Text.Font = GameFont.Tiny;
            Widgets.Label(
                new Rect(agentCard.x + RimMindUI.Padding, innerY, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                "RimMind.UI.Hub.AgentSummary".Translate());
            Text.Font = GameFont.Medium;
            GUI.color = activeCount > 0 ? RimMindUI.ColorActive : RimMindUI.ColorMuted;
            Widgets.Label(
                new Rect(agentCard.x + RimMindUI.Padding, innerY + RimMindUI.LineHeight, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                $"{activeCount} / {agentCount}");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            Rect queueCard = new Rect(rect.x + cardW + RimMindUI.Padding, y, cardW, cardH);
            Widgets.DrawBoxSolid(queueCard, RimMindUI.ColorCardBg);
            innerY = queueCard.y + RimMindUI.Padding;
            GUI.color = RimMindUI.ColorSectionTitle;
            Text.Font = GameFont.Tiny;
            Widgets.Label(
                new Rect(queueCard.x + RimMindUI.Padding, innerY, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                "RimMind.UI.Hub.QueueState".Translate());
            Text.Font = GameFont.Medium;
            bool isQueueRunning = queue != null && !queue.IsPaused;
            GUI.color = isQueueRunning ? RimMindUI.ColorActive : RimMindUI.ColorPaused;
            Widgets.Label(
                new Rect(queueCard.x + RimMindUI.Padding, innerY + RimMindUI.LineHeight, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                queueText);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            y += cardH + RimMindUI.SectionGap;

            y = RimMindUI.DrawSectionHeader(rect, y - rect.y, "RimMind.UI.Hub.SelectedPawn".Translate()) + rect.y;
            string pawnText = selectedPawn?.LabelShortCap ?? "RimMind.UI.Hub.NoPawn".Translate();
            y = RimMindUI.DrawKeyValueRow(rect, y - rect.y, "RimMind.UI.Hub.SelectedPawn".Translate(), pawnText) + rect.y;
            y = RimMindUI.DrawKeyValueRow(rect, y - rect.y, "RimMind.UI.Hub.PendingRequests".Translate(), RequestOverlay.Pending.Count.ToString()) + rect.y;

            y += RimMindUI.SectionGap;
            y = RimMindUI.DrawSectionHeader(rect, y - rect.y, "RimMind.UI.Hub.QuickActions".Translate()) + rect.y;

            DebugCenterToolGrid.Draw(
                new Rect(rect.x, y, rect.width, RimMindUI.BtnHeight * 2f + RimMindUI.Padding),
                ("RimMind.UI.Hub.AgentFlowLab", () => Find.WindowStack.Add(new Window_AgentFlowLab(selectedPawn))),
                ("RimMind.UI.Hub.AgentProgress", () => Find.WindowStack.Add(new Window_AgentProgressFloat())),
                ("RimMind.UI.Hub.AgentState", () => Find.WindowStack.Add(new Window_AgentStateDebug(selectedPawn))),
                ("RimMind.UI.Hub.AgentMode", () => Find.WindowStack.Add(new Window_AgentModeDebug(selectedPawn))));
        }
    }
}
