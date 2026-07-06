using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.UI.DebugCenter;
using RimMind.Infrastructure.UI.Layout;
using RimMind.Infrastructure.Verse;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public sealed class AgentDetailPanelDrawer
    {
        private readonly AgentActivityStreamDrawer _activityDrawer = new();
        private readonly AgentChatPanelDrawer _chatDrawer = new();

        public void Draw(AgentPageRects layout, Pawn? selectedPawn, ref string chatDraft, RimMindLayoutScope scope)
        {
            scope.Record(layout.Detail, "Agents:DetailPanel");
            Widgets.DrawBoxSolid(layout.Detail, RimMindUI.ColorCardBg);

            if (selectedPawn == null)
            {
                RimMindUI.DrawEmptyState(layout.Detail, "RimMind.UI.AgentStateDebug.NoPawn".Translate());
                return;
            }

            var comp = CompPawnAgent.GetComp(selectedPawn);
            DrawDetailHeader(layout.Status, selectedPawn, comp?.Agent);

            if (comp?.Agent == null)
            {
                var model = AgentPageViewModel.PendingCreation(
                    selectedPawn.LabelShortCap,
                    RequestOverlay.Pending.Count);

                if (Widgets.ButtonText(
                    new Rect(layout.Actions.x, layout.Actions.y, 160f, RimMindUI.BtnHeight),
                    "RimMind.UI.AgentsPage.CreateStart".Translate()))
                {
                    if (comp != null && comp.EnsureAgentCreated())
                        SafeTransitionTo(comp.Agent, AgentState.Active);
                    else
                        Messages.Message("RimMind.UI.AgentsPage.CreateFailed".Translate(),
                            MessageTypeDefOf.RejectInput, false);
                }

                _activityDrawer.Draw(
                    layout.Activity,
                    "RimMind.UI.AgentsPage.Pending".Translate(),
                    model.PendingRequests,
                    model.TraceRows,
                    scope);
                return;
            }

            var agent = comp.Agent;
            var agentModel = AgentPageViewModel.FromState(
                selectedPawn.LabelShortCap,
                agent.State,
                RequestOverlay.Pending.Count,
                requestRows: 0);
            DrawActions(layout.Actions, agent);
            _activityDrawer.Draw(
                layout.Activity,
                agentModel.State,
                agentModel.PendingRequests,
                agentModel.TraceRows,
                scope);

            if (agent.State == AgentState.Active || agent.State == AgentState.Paused)
                _chatDrawer.Draw(layout.Chat, selectedPawn, ref chatDraft, scope);
        }

        private static void DrawDetailHeader(Rect rect, Pawn pawn, IAgentControl? agent)
        {
            Rect inner = rect.ContractedBy(RimMindUI.Padding);

            Text.Font = GameFont.Medium;
            GUI.color = RimMindUI.ColorHeader;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, RimMindUI.LineHeight), pawn.LabelShortCap);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            string stateLabel = "RimMind.UI.AgentsPage.Pending".Translate();
            AgentState? state = null;
            if (agent != null)
            {
                state = agent.State;
                stateLabel = AgentActivityStreamDrawer.StateLabel(agent.State);
            }

            var (textColor, bgColor) = state.HasValue
                ? RimMindUI.GetStateBadgeColors(state.Value)
                : RimMindUI.GetStateBadgeColors(AgentState.Dormant, isPendingCreation: true);
            RimMindUI.DrawStatusBadge(inner, inner.y + RimMindUI.LineHeight + RimMindUI.Padding - inner.y,
                "RimMind.UI.AgentsPage.State".Translate() + ": " + stateLabel, textColor, bgColor);
        }

        private static void DrawActions(Rect rect, IAgentControl agent)
        {
            float buttonW = Mathf.Min(130f, (rect.width - RimMindUI.Padding * 2f) / 3f);
            float x = rect.x;

            switch (agent.State)
            {
                case AgentState.Active:
                    if (Widgets.ButtonText(new Rect(x, rect.y, buttonW, RimMindUI.BtnHeight),
                        "RimMind.UI.AgentsPage.Pause".Translate()))
                        SafeTransitionTo(agent, AgentState.Paused);
                    break;
                case AgentState.Paused:
                    if (Widgets.ButtonText(new Rect(x, rect.y, buttonW, RimMindUI.BtnHeight),
                        "RimMind.UI.AgentsPage.Resume".Translate()))
                        SafeTransitionTo(agent, AgentState.Active);
                    break;
                case AgentState.Dormant:
                    if (Widgets.ButtonText(new Rect(x, rect.y, buttonW, RimMindUI.BtnHeight),
                        "RimMind.UI.AgentsPage.Activate".Translate()))
                        SafeTransitionTo(agent, AgentState.Active);
                    break;
                case AgentState.Terminated:
                    if (Widgets.ButtonText(new Rect(x, rect.y, buttonW, RimMindUI.BtnHeight),
                        "RimMind.UI.AgentsPage.Restart".Translate()))
                        SafeTransitionTo(agent, AgentState.Active);
                    break;
            }

            x += buttonW + RimMindUI.Padding;
            if (agent.State == AgentState.Active || agent.State == AgentState.Paused)
            {
                if (Widgets.ButtonText(new Rect(x, rect.y, buttonW, RimMindUI.BtnHeight),
                    "RimMind.UI.AgentsPage.ForceThink".Translate()))
                {
                    agent.ForceThink();
                }

                x += buttonW + RimMindUI.Padding;
                if (Widgets.ButtonText(new Rect(x, rect.y, buttonW, RimMindUI.BtnHeight),
                    "RimMind.UI.AgentsPage.OpenRequests".Translate()))
                {
                    Find.WindowStack.Add(Window_RimMindHub.OpenAIRequests());
                }
            }
        }

        private static void SafeTransitionTo(IAgentControl? agent, AgentState target)
        {
            if (agent == null) return;
            agent.TransitionTo(target);
        }
    }
}
