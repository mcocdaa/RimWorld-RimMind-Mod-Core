using System;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Infrastructure.UI.DebugCenter.Overview;
using RimMind.Presentation.UI.Layout;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation.Runtime.Services;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class OverviewDebugCenterPageDrawer : IRuntimeBoundDebugCenterPageDrawer
    {
        private const float OverviewContentHeight = 540f;

        private IAgentLoopScheduler? _agentLoopScheduler;
        private IAIRequestQueue? _requestQueue;
        private Vector2 _scrollPosition;

        public IDisposable? Bind(RuntimeServiceScope scope)
        {
            _agentLoopScheduler = scope.GetOptional<IAgentLoopScheduler>();
            _requestQueue = scope.GetOptional<IAIRequestQueue>();
            return null;
        }

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            Pawn? selectedPawn = context.SelectedPawn ?? Find.Selector.SingleSelectedThing as Pawn;
            DebugCenterOverviewModel model = BuildModel(selectedPawn);

            scope.Record(rect, "Hub:Overview:ScrollViewport");
            Rect viewRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(1f, rect.width - 16f),
                Mathf.Max(rect.height + 1f, OverviewContentHeight));
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            DrawOverviewContent(viewRect, context, selectedPawn, model);
            Widgets.EndScrollView();
        }

        private static void DrawOverviewContent(
            Rect rect,
            DebugCenterPageContext context,
            Pawn? selectedPawn,
            DebugCenterOverviewModel model)
        {
            Rect[] cards = CalculateCardRects(rect);
            DrawOverviewCard(cards[0], "RimMind.Context.IncludeHealth".Translate(), BuildHealthText(model), ResolveHealthColor(model));

            DrawOverviewCard(cards[1], "RimMind.UI.Hub.AgentSummary".Translate(), BuildAgentStateSummary(model), model.ActiveAgents > 0 ? RimMindUI.ColorActive : RimMindUI.ColorMuted);

            bool isQueueRunning = model.QueueState == "RimMind.Settings.QueueRunning".Translate();
            DrawOverviewCard(cards[2], "RimMind.UI.Hub.QueueState".Translate(), model.QueueState, isQueueRunning ? RimMindUI.ColorActive : RimMindUI.ColorPaused);

            DrawOverviewCard(cards[3], "RimMind.UI.Hub.SelectedPawn".Translate(), model.SelectedObject, selectedPawn == null ? RimMindUI.ColorMuted : RimMindUI.ColorValue);

            float y = cards[3].yMax + RimMindUI.SectionGap;
            y = RimMindUI.DrawKeyValueRow(rect, y, "RimMind.UI.Hub.PendingRequests".Translate(), model.PendingRequests.ToString());
            y = RimMindUI.DrawKeyValueRow(rect, y, "RimMind.UI.Hub.AgentLoop".Translate(), BuildAgentLoopSummary(model));
            y = RimMindUI.DrawKeyValueRow(rect, y, "RimMind.UI.Hub.AgentLoopLastTick".Translate(), BuildLastAgentLoopTick(model));
            y = RimMindUI.DrawKeyValueRow(rect, y, "RimMind.UI.Hub.AgentLoopFaults".Translate(), model.AgentLoopFaults.ToString());

            y += RimMindUI.SectionGap;
            y = RimMindUI.DrawSectionHeader(rect, y, "RimMind.UI.Hub.Lifecycle.Title".Translate());
            y = DrawLifecycleDiagnostics(rect, y, model);

            y += RimMindUI.SectionGap;
            y = RimMindUI.DrawSectionHeader(rect, y, "RimMind.UI.Hub.QuickActions".Translate());

            DebugCenterToolGrid.Draw(
                new Rect(rect.x, y, rect.width, RimMindUI.BtnHeight * 2f + RimMindUI.Padding),
                scope: null,
                ("RimMind.UI.Hub.Tab.Agents", () => context.Navigation.GoTo("agents")),
                ("RimMind.UI.Hub.Tab.AIRequests", () => context.Navigation.GoTo("ai_requests")),
                ("RimMind.UI.Hub.Tab.ToolCalls", () => context.Navigation.GoTo("tool_calls")),
                ("RimMind.UI.Hub.Tab.Mechanisms", () => context.Navigation.GoTo("mechanisms")));
        }

        private DebugCenterOverviewModel BuildModel(Pawn? selectedPawn)
        {
            AgentLoopSnapshot loop = _agentLoopScheduler?.GetSnapshot() ?? AgentLoopSnapshot.Empty;

            string queueText = _requestQueue == null
                ? "RimMind.UI.Hub.QueueMissing".Translate()
                : _requestQueue.IsPaused
                    ? "RimMind.Settings.QueuePaused".Translate()
                    : "RimMind.Settings.QueueRunning".Translate();

            var model = new DebugCenterOverviewModel(
                loop.ActiveAgents,
                loop.PausedAgents,
                loop.DormantAgents,
                loop.TerminatedAgents,
                RequestOverlay.Pending.Count,
                queueText,
                selectedPawn?.LabelShortCap ?? "RimMind.UI.Hub.NoPawn".Translate(),
                loop.RegisteredPawnAgents,
                loop.RegisteredScopedAgents,
                loop.LastTick,
                loop.FaultedAgents);
            model.AttachLifecycleDiagnostics(
                RuntimeServiceHub.Shared.GetDiagnostics(),
                GameServiceHub.Shared.GetDiagnostics());
            return model;
        }

        private static float DrawLifecycleDiagnostics(
            Rect rect,
            float y,
            DebugCenterOverviewModel model)
        {
            float gap = RimMindUI.Padding;
            float columnWidth = (rect.width - gap) / 2f;
            const float diagnosticsHeight = 124f;
            Rect runtimeRect = new Rect(rect.x, y, columnWidth, diagnosticsHeight);
            Rect gameRect = new Rect(runtimeRect.xMax + gap, y, columnWidth, diagnosticsHeight);
            Widgets.DrawBoxSolid(runtimeRect, RimMindUI.ColorCardBg);
            Widgets.DrawBoxSolid(gameRect, RimMindUI.ColorCardBg);

            float runtimeY = RimMindUI.Padding;
            runtimeY = DrawLifecycleRow(runtimeRect, runtimeY, "RimMind.UI.Hub.Lifecycle.RuntimeState", LocalizeLifecycleState(model.RuntimeDiagnostics?.State));
            runtimeY = DrawLifecycleRow(runtimeRect, runtimeY, "RimMind.UI.Hub.Lifecycle.Generation", model.RuntimeGeneration.ToString());
            runtimeY = DrawLifecycleRow(runtimeRect, runtimeY, "RimMind.UI.Hub.Lifecycle.ServiceCount", model.RuntimeServiceCount.ToString());
            runtimeY = DrawLifecycleRow(runtimeRect, runtimeY, "RimMind.UI.Hub.Lifecycle.PublishedAt", FormatPublished(model.RuntimePublishedAtUtc));
            runtimeY = DrawLifecycleRow(runtimeRect, runtimeY, "RimMind.UI.Hub.Lifecycle.RuntimeId", model.RuntimeId.ToString());
            runtimeY = DrawLifecycleRow(runtimeRect, runtimeY, "RimMind.UI.Hub.Lifecycle.LastFailure", FormatFailure(model.LastBuildFailureSummary));
            DrawLifecycleRow(runtimeRect, runtimeY, "RimMind.UI.Hub.Lifecycle.StaleDiscards", model.StaleCompletionDiscardCount.ToString());

            float gameY = RimMindUI.Padding;
            gameY = DrawLifecycleRow(gameRect, gameY, "RimMind.UI.Hub.Lifecycle.GameState", LocalizeLifecycleState(model.GameDiagnostics?.State));
            gameY = DrawLifecycleRow(gameRect, gameY, "RimMind.UI.Hub.Lifecycle.Generation", model.GameGeneration.ToString());
            gameY = DrawLifecycleRow(gameRect, gameY, "RimMind.UI.Hub.Lifecycle.ServiceCount", model.GameServiceCount.ToString());
            DrawLifecycleRow(gameRect, gameY, "RimMind.UI.Hub.Lifecycle.PublishedAt", FormatPublished(model.GamePublishedAtUtc));
            return y + diagnosticsHeight;
        }

        private static float DrawLifecycleRow(Rect column, float y, string labelKey, string value)
        {
            const float rowHeight = 16f;
            Rect row = new Rect(column.x + RimMindUI.Padding, column.y + y, column.width - RimMindUI.Padding * 2f, rowHeight);
            string text = labelKey.Translate() + ": " + value;
            Text.Font = GameFont.Tiny;
            Widgets.Label(row, text);
            TooltipHandler.TipRegion(row, text);
            Text.Font = GameFont.Small;
            return y + rowHeight;
        }

        private static string FormatPublished(DateTimeOffset? publishedAtUtc)
            => publishedAtUtc?.ToString("u") ?? "RimMind.UI.Hub.Lifecycle.Never".Translate();

        private static string FormatFailure(string? failure)
            => string.IsNullOrWhiteSpace(failure)
                ? "RimMind.UI.Hub.Lifecycle.None".Translate()
                : failure;

        private static string LocalizeLifecycleState(RuntimeLifecycleState? state)
            => state switch
            {
                RuntimeLifecycleState.NeverPublished => "RimMind.UI.Lifecycle.NeverPublished".Translate(),
                RuntimeLifecycleState.Building => "RimMind.UI.Lifecycle.Building".Translate(),
                RuntimeLifecycleState.Running => "RimMind.UI.Lifecycle.Running".Translate(),
                RuntimeLifecycleState.Stopped => "RimMind.UI.Lifecycle.Stopped".Translate(),
                RuntimeLifecycleState.Failed => "RimMind.UI.Lifecycle.Failed".Translate(),
                _ => string.Empty
            };

        private static string LocalizeLifecycleState(GameLifecycleState? state)
            => state switch
            {
                GameLifecycleState.NeverPublished => "RimMind.UI.Lifecycle.NeverPublished".Translate(),
                GameLifecycleState.Running => "RimMind.UI.Lifecycle.Running".Translate(),
                GameLifecycleState.Stopped => "RimMind.UI.Lifecycle.Stopped".Translate(),
                GameLifecycleState.Failed => "RimMind.UI.Lifecycle.Failed".Translate(),
                _ => string.Empty
            };

        private static Rect[] CalculateCardRects(Rect rect)
        {
            float cardW = (rect.width - RimMindUI.Padding) / 2f;
            const float cardH = 76f;
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
            if (model.AgentLoopFaults > 0)
                return $"{model.AgentLoopFaults} {"RimMind.UI.AgentsPage.Trace.Error".Translate()}";
            if (model.PausedAgents > 0)
                return $"{model.PausedAgents} {"RimMind.Agent.State.Paused".Translate()}";
            return model.ActiveAgents > 0
                ? "RimMind.Agent.State.Active".Translate()
                : "RimMind.Prompt.Health.Healthy".Translate();
        }

        private static string BuildAgentStateSummary(DebugCenterOverviewModel model)
            => $"{model.ActiveAgents} {"RimMind.Agent.State.Active".Translate()} / "
                + $"{model.PausedAgents} {"RimMind.Agent.State.Paused".Translate()} / "
                + $"{model.DormantAgents} {"RimMind.Agent.State.Dormant".Translate()} / "
                + $"{model.TerminatedAgents} {"RimMind.Agent.State.Terminated".Translate()}";

        private static string BuildAgentLoopSummary(DebugCenterOverviewModel model)
            => $"{model.RegisteredPawnAgents} {"RimMind.UI.Hub.AgentLoopPawn".Translate()} / "
                + $"{model.RegisteredScopedAgents} {"RimMind.UI.Hub.AgentLoopScoped".Translate()}";

        private static string BuildLastAgentLoopTick(DebugCenterOverviewModel model)
            => model.LastAgentLoopTick < 0
                ? "RimMind.UI.Hub.AgentLoopNeverRun".Translate()
                : model.LastAgentLoopTick.ToString();

        private static Color ResolveHealthColor(DebugCenterOverviewModel model)
        {
            if (model.AgentLoopFaults > 0)
                return RimMindUI.ColorError;
            if (model.PausedAgents > 0)
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
            Text.Font = GameFont.Small;
            GUI.color = valueColor;
            Widgets.Label(
                new Rect(card.x + RimMindUI.Padding, innerY + RimMindUI.LineHeight, card.width - RimMindUI.Padding * 2, card.height - RimMindUI.LineHeight - RimMindUI.Padding * 2f),
                value);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
