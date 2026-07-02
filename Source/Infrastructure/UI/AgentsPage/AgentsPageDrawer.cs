using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.Verse;
using RimMind.Infrastructure.UI;
using RimMind.Infrastructure.UI.DebugCenter;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public sealed class AgentsPageDrawer
    {
        private string _chatDraft = string.Empty;
        private string? _listSelectedPawnId;
        private Vector2 _listScrollPos;
        private Vector2 _activityScrollPos;
        private readonly List<AgentListItem> _agents = new();
        private readonly Dictionary<string, Pawn> _pawnById = new();

        public void Draw(Rect rect, Pawn? hubSelectedPawn)
        {
            AgentPageLayoutRects layout = DebugCenterLayout.CalculateAgentPage(rect);

            DrawList(layout.List, hubSelectedPawn);

            // Resolve detail pawn: list selection takes priority, fall back to hub selection
            Pawn? detailPawn = ResolveDetailPawn(hubSelectedPawn);
            DrawDetail(layout, detailPawn);
        }

        private Pawn? ResolveDetailPawn(Pawn? hubSelectedPawn)
        {
            if (_listSelectedPawnId != null
                && _pawnById.TryGetValue(_listSelectedPawnId, out Pawn? listPawn))
                return listPawn;

            return hubSelectedPawn;
        }

        private void DrawList(Rect rect, Pawn? hubSelectedPawn)
        {
            Widgets.DrawBoxSolid(rect, RimMindUI.ColorSectionBg);

            // Reuse collections to reduce GC pressure
            _agents.Clear();
            _pawnById.Clear();

            var map = Find.CurrentMap;
            if (map != null)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var comp = CompPawnAgent.GetComp(pawn);
                    if (comp?.Agent == null) continue;
                    var state = comp.Agent.State;
                    string id = pawn.ThingID;
                    _agents.Add(AgentListItem.ExistingPawn(id, pawn.LabelShortCap, state));
                    _pawnById[id] = pawn;
                }
            }

            string? pendingId = hubSelectedPawn != null
                ? hubSelectedPawn.ThingID : null;
            string? pendingLabel = hubSelectedPawn?.LabelShortCap;

            var groups = AgentListBuilder.Build(_agents, pendingId, pendingLabel);

            // Sync list selection with hub's external selectedPawn only on first draw
            if (hubSelectedPawn != null && _listSelectedPawnId == null)
                _listSelectedPawnId = hubSelectedPawn.ThingID;

            Rect innerRect = rect.ContractedBy(RimMindUI.Padding);
            float contentH = Mathf.Max(innerRect.height + 1f, CalcListHeight(groups));
            var (bodyRect, _) = RimMindUI.BeginScrollView(innerRect, ref _listScrollPos, contentH);

            float y = 0f;

            // Active section
            y = RimMindUI.DrawSectionHeader(bodyRect, y,
                "RimMind.UI.AgentsPage.Active".Translate() + $" ({groups.Active.Count})");
            foreach (var item in groups.Active)
                y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width, RimMindUI.LineHeight), item);

            y += RimMindUI.Padding;

            // Paused section
            y = RimMindUI.DrawSectionHeader(bodyRect, y,
                "RimMind.UI.AgentsPage.Paused".Translate() + $" ({groups.Paused.Count})");
            foreach (var item in groups.Paused)
                y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width, RimMindUI.LineHeight), item);

            y += RimMindUI.Padding;

            // Pending creation section
            if (groups.PendingCreation.Count > 0)
            {
                y = RimMindUI.DrawSectionHeader(bodyRect, y,
                    "RimMind.UI.AgentsPage.Pending".Translate());
                foreach (var item in groups.PendingCreation)
                    y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width, RimMindUI.LineHeight), item);
            }

            // Other section (Dormant/Terminated)
            if (groups.Other.Count > 0)
            {
                y += RimMindUI.Padding;
                y = RimMindUI.DrawSectionHeader(bodyRect, y,
                    "RimMind.UI.AgentsPage.Other".Translate() + $" ({groups.Other.Count})");
                foreach (var item in groups.Other)
                    y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width, RimMindUI.LineHeight), item);
            }

            Widgets.EndScrollView();
        }

        private float DrawAgentRow(Rect rect, AgentListItem item)
        {
            var (textColor, bgColor) = RimMindUI.GetStateBadgeColors(item.State, item.IsPendingCreation);

            bool isSelected = item.Id == _listSelectedPawnId;

            // Three mutually exclusive visual states: selected > hover > normal
            if (isSelected)
                Widgets.DrawBoxSolid(rect, RimMindUI.ColorTabActive);
            else if (Mouse.IsOver(rect))
                Widgets.DrawBoxSolid(rect, RimMindUI.ColorTabHover);
            else
                Widgets.DrawBoxSolid(rect, bgColor);

            GUI.color = textColor;
            Widgets.Label(rect, item.Label);
            GUI.color = Color.white;

            // Click to select pawn in list
            if (Widgets.ButtonInvisible(rect))
            {
                _listSelectedPawnId = item.Id;
                if (_pawnById.TryGetValue(item.Id, out Pawn? pawn))
                    Find.Selector.Select(pawn, false, true);
            }

            return rect.yMax;
        }

        private static float CalcListHeight(AgentListGroups groups)
        {
            float h = 0f;
            float sectionHeaderH = RimMindUI.LineHeight + RimMindUI.SectionGap * 0.5f;
            float rowH = RimMindUI.LineHeight;

            h += sectionHeaderH + groups.Active.Count * rowH + RimMindUI.Padding;
            h += sectionHeaderH + groups.Paused.Count * rowH + RimMindUI.Padding;
            if (groups.PendingCreation.Count > 0)
                h += sectionHeaderH + groups.PendingCreation.Count * rowH;
            if (groups.Other.Count > 0)
                h += RimMindUI.Padding + sectionHeaderH + groups.Other.Count * rowH;

            return h;
        }

        private void DrawDetail(AgentPageLayoutRects layout, Pawn? selectedPawn)
        {
            Rect rect = layout.Detail;
            Widgets.DrawBoxSolid(rect, RimMindUI.ColorCardBg);

            if (selectedPawn == null)
            {
                RimMindUI.DrawEmptyState(rect, "RimMind.UI.AgentStateDebug.NoPawn".Translate());
                return;
            }

            var comp = CompPawnAgent.GetComp(selectedPawn);
            DrawDetailHeader(layout.Header, selectedPawn, comp?.Agent);

            if (comp?.Agent == null)
            {
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

                DrawActivity(layout.Activity, "RimMind.UI.AgentsPage.Pending".Translate(), RequestOverlay.Pending.Count);
                return;
            }

            var agent = comp.Agent;
            DrawActions(layout.Actions, agent);
            DrawActivity(layout.Activity, StateLabel(agent.State), RequestOverlay.Pending.Count);

            if (agent.State == AgentState.Active || agent.State == AgentState.Paused)
                DrawChat(layout.Chat, selectedPawn);
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
                stateLabel = StateLabel(agent.State);
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

        private void DrawActivity(Rect rect, string stateLabel, int pendingRequests)
        {
            Widgets.DrawBoxSolid(rect, RimMindUI.ColorSectionBg);
            Rect inner = rect.ContractedBy(RimMindUI.Padding);

            float contentHeight = RimMindUI.LineHeight * 5f + RimMindUI.SectionGap;
            var (bodyRect, _) = RimMindUI.BeginScrollView(inner, ref _activityScrollPos,
                Mathf.Max(inner.height + 1f, contentHeight));

            float y = RimMindUI.DrawSectionHeader(bodyRect, 0f,
                "RimMind.UI.AgentsPage.Activity".Translate());
            y = RimMindUI.DrawKeyValueRow(bodyRect, y,
                "RimMind.UI.AgentsPage.State".Translate(), stateLabel);
            y = RimMindUI.DrawKeyValueRow(bodyRect, y,
                "RimMind.UI.Hub.PendingRequests".Translate(), pendingRequests.ToString());
            RimMindUI.DrawWrappedLabel(bodyRect, y,
                "RimMind.UI.AgentsPage.Activity.Empty".Translate(), RimMindUI.ColorMuted);

            Widgets.EndScrollView();
        }

        private static string StateLabel(AgentState state)
        {
            return state switch
            {
                AgentState.Active => "RimMind.UI.AgentsPage.Active".Translate(),
                AgentState.Paused => "RimMind.UI.AgentsPage.Paused".Translate(),
                AgentState.Terminated => "RimMind.UI.AgentsPage.Terminated".Translate(),
                _ => "RimMind.UI.AgentsPage.Dormant".Translate()
            };
        }

        private void DrawChat(Rect rect, Pawn pawn)
        {
            _chatDraft = Widgets.TextField(
                new Rect(rect.x, rect.y, rect.width - 80f, rect.height), _chatDraft);
            if (Widgets.ButtonText(
                new Rect(rect.xMax - 74f, rect.y, 74f, rect.height),
                "RimMind.UI.AgentsPage.Send".Translate()))
                SendAgentMessage(pawn);
        }

        private void SendAgentMessage(Pawn pawn)
        {
            if (string.IsNullOrWhiteSpace(_chatDraft)) return;
            Messages.Message("RimMind.UI.AgentsPage.MessageUnavailable".Translate(),
                MessageTypeDefOf.RejectInput, false);
        }

        private static void SafeTransitionTo(IAgentControl? agent, AgentState target)
        {
            if (agent == null) return;
            agent.TransitionTo(target);
        }
    }
}
