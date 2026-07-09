using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.UI.DebugCenter.Overview;
using RimMind.Presentation.UI.Layout;
using RimMind.Infrastructure.Verse;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class OverviewDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            Pawn? selectedPawn = context.SelectedPawn ?? Find.Selector.SingleSelectedThing as Pawn;
            DebugCenterOverviewModel model = BuildModel(selectedPawn);

            Rect[] cards = CalculateCardRects(rect);
            DrawOverviewCard(cards[0], "RimMind.Context.IncludeHealth".Translate(), BuildHealthText(model), ResolveHealthColor(model));
            scope.Record(cards[0], "Hub:Overview:Health");

            DrawOverviewCard(cards[1], "RimMind.UI.Hub.AgentSummary".Translate(), model.AgentSummary, model.ActiveAgents > 0 ? RimMindUI.ColorActive : RimMindUI.ColorMuted);
            scope.Record(cards[1], "Hub:Overview:Agents");

            bool isQueueRunning = model.QueueState == "RimMind.Settings.QueueRunning".Translate();
            DrawOverviewCard(cards[2], "RimMind.UI.Hub.QueueState".Translate(), model.QueueState, isQueueRunning ? RimMindUI.ColorActive : RimMindUI.ColorPaused);
            scope.Record(cards[2], "Hub:Overview:Queue");

            DrawOverviewCard(cards[3], "RimMind.UI.Hub.SelectedPawn".Translate(), model.SelectedObject, selectedPawn == null ? RimMindUI.ColorMuted : RimMindUI.ColorValue);
            scope.Record(cards[3], "Hub:Overview:Selection");

            float y = cards[3].yMax + RimMindUI.SectionGap;
            y = RimMindUI.DrawKeyValueRow(rect, y - rect.y, "RimMind.UI.Hub.PendingRequests".Translate(), model.PendingRequests.ToString()) + rect.y;

            y += RimMindUI.SectionGap;
            y = RimMindUI.DrawSectionHeader(rect, y - rect.y, "RimMind.UI.Hub.QuickActions".Translate()) + rect.y;

            DebugCenterToolGrid.Draw(
                new Rect(rect.x, y, rect.width, RimMindUI.BtnHeight * 2f + RimMindUI.Padding),
                scope,
                ("RimMind.UI.Hub.AgentFlowLab", () => Find.WindowStack.Add(new Window_AgentFlowLab(selectedPawn))),
                ("RimMind.UI.Hub.AgentProgress", () => Find.WindowStack.Add(new Window_AgentProgressFloat())),
                ("RimMind.UI.Hub.AgentState", () => Find.WindowStack.Add(new Window_AgentStateDebug(selectedPawn))),
                ("RimMind.UI.Hub.AgentMode", () => Find.WindowStack.Add(new Window_AgentModeDebug(selectedPawn))));
        }

        private static DebugCenterOverviewModel BuildModel(Pawn? selectedPawn)
        {
            int agentCount = 0;
            int activeCount = 0;
            int pausedCount = 0;
            int pendingCount = 0;
            int errorCount = 0;
            var map = Find.CurrentMap;

            if (map != null)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var agent = CompPawnAgent.GetComp(pawn)?.Agent;
                    if (agent == null) continue;
                    agentCount++;
                    switch (agent.State)
                    {
                        case AgentState.Active:
                            activeCount++;
                            break;
                        case AgentState.Paused:
                            pausedCount++;
                            break;
                        case AgentState.Terminated:
                            errorCount++;
                            break;
                        default:
                            pendingCount++;
                            break;
                    }
                }
            }

            var queue = RimMindServiceLocator.TryGet<IAIRequestQueue>();
            string queueText = queue == null
                ? "RimMind.UI.Hub.QueueMissing".Translate()
                : queue.IsPaused
                    ? "RimMind.Settings.QueuePaused".Translate()
                    : "RimMind.Settings.QueueRunning".Translate();

            return new DebugCenterOverviewModel(
                activeCount,
                pausedCount,
                pendingCount,
                errorCount,
                RequestOverlay.Pending.Count,
                queueText,
                selectedPawn?.LabelShortCap ?? "RimMind.UI.Hub.NoPawn".Translate());
        }

        private static Rect[] CalculateCardRects(Rect rect)
        {
            float cardW = (rect.width - RimMindUI.Padding) / 2f;
            const float cardH = 72f;
            return new[]
            {
                new Rect(rect.x, rect.y, cardW, cardH),
                new Rect(rect.x + cardW + RimMindUI.Padding, rect.y, cardW, cardH),
                new Rect(rect.x, rect.y + cardH + RimMindUI.Padding, cardW, cardH),
                new Rect(rect.x + cardW + RimMindUI.Padding, rect.y + cardH + RimMindUI.Padding, cardW, cardH)
            };
        }

        private static string BuildHealthText(DebugCenterOverviewModel model)
        {
            if (model.ErrorAgents > 0)
                return $"{model.ErrorAgents} {"RimMind.UI.AgentsPage.Trace.Error".Translate()}";
            if (model.PendingAgents > 0)
                return $"{model.PendingAgents} {"RimMind.UI.Hub.PendingRequests".Translate()}";
            return model.ActiveAgents > 0
                ? "RimMind.Agent.State.Active".Translate()
                : "RimMind.Prompt.Health.Healthy".Translate();
        }

        private static Color ResolveHealthColor(DebugCenterOverviewModel model)
        {
            if (model.ErrorAgents > 0)
                return RimMindUI.ColorError;
            if (model.PendingAgents > 0 || model.PausedAgents > 0)
                return RimMindUI.ColorPaused;
            return model.ActiveAgents > 0 ? RimMindUI.ColorActive : RimMindUI.ColorMuted;
        }

        private static void DrawOverviewCard(Rect card, string title, string value, Color valueColor)
        {
            Widgets.DrawBoxSolid(card, RimMindUI.ColorCardBg);
            float innerY = card.y + RimMindUI.Padding;
            GUI.color = RimMindUI.ColorSectionTitle;
            Text.Font = GameFont.Tiny;
            Widgets.Label(
                new Rect(card.x + RimMindUI.Padding, innerY, card.width - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                title);
            Text.Font = GameFont.Medium;
            GUI.color = valueColor;
            Widgets.Label(
                new Rect(card.x + RimMindUI.Padding, innerY + RimMindUI.LineHeight, card.width - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                value);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
