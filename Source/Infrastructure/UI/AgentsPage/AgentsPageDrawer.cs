using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.Verse;
using RimMind.Infrastructure.UI;
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
        private readonly List<AgentListItem> _agents = new();
        private readonly Dictionary<string, Pawn> _pawnById = new();

        public void Draw(Rect rect, Pawn? hubSelectedPawn)
        {
            Rect left = new(rect.x, rect.y, 260f, rect.height);
            Rect right = new(left.xMax + 8f, rect.y, rect.width - left.width - 8f, rect.height);

            DrawList(left, hubSelectedPawn);

            // Resolve detail pawn: list selection takes priority, fall back to hub selection
            Pawn? detailPawn = ResolveDetailPawn(hubSelectedPawn);
            DrawDetail(right, detailPawn);
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

            float contentH = CalcListHeight(groups);
            var (bodyRect, viewRect) = RimMindUI.BeginScrollView(rect, ref _listScrollPos, contentH);

            float y = 0f;

            // Active section
            y = RimMindUI.DrawSectionHeader(bodyRect, y,
                "RimMind.UI.AgentsPage.Active".Translate() + $" ({groups.Active.Count})");
            foreach (var item in groups.Active)
                y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width - 16f, RimMindUI.LineHeight), item);

            y += RimMindUI.Padding;

            // Paused section
            y = RimMindUI.DrawSectionHeader(bodyRect, y,
                "RimMind.UI.AgentsPage.Paused".Translate() + $" ({groups.Paused.Count})");
            foreach (var item in groups.Paused)
                y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width - 16f, RimMindUI.LineHeight), item);

            y += RimMindUI.Padding;

            // Pending creation section
            if (groups.PendingCreation.Count > 0)
            {
                y = RimMindUI.DrawSectionHeader(bodyRect, y,
                    "RimMind.UI.AgentsPage.Pending".Translate());
                foreach (var item in groups.PendingCreation)
                    y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width - 16f, RimMindUI.LineHeight), item);
            }

            // Other section (Dormant/Terminated)
            if (groups.Other.Count > 0)
            {
                y += RimMindUI.Padding;
                y = RimMindUI.DrawSectionHeader(bodyRect, y,
                    "RimMind.UI.AgentsPage.Other".Translate() + $" ({groups.Other.Count})");
                foreach (var item in groups.Other)
                    y = DrawAgentRow(new Rect(bodyRect.x, y, bodyRect.width - 16f, RimMindUI.LineHeight), item);
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

        private void DrawDetail(Rect rect, Pawn? selectedPawn)
        {
            if (selectedPawn == null)
            {
                RimMindUI.DrawEmptyState(rect, "RimMind.UI.AgentStateDebug.NoPawn".Translate());
                return;
            }

            float y = rect.y;
            float chatZoneTop = rect.yMax - 34f;

            // Pawn name header
            y = RimMindUI.DrawSectionHeader(rect, y - rect.y, selectedPawn.LabelShortCap) + rect.y;
            y += RimMindUI.Padding;

            var comp = CompPawnAgent.GetComp(selectedPawn);
            if (comp?.Agent == null)
            {
                // Pending-created state: show Create/Start button
                if (y + RimMindUI.BtnHeight < chatZoneTop)
                {
                    if (Widgets.ButtonText(new Rect(rect.x, y, 160f, RimMindUI.BtnHeight),
                        "RimMind.UI.AgentsPage.CreateStart".Translate()))
                    {
                        if (comp != null && comp.EnsureAgentCreated())
                            SafeTransitionTo(comp.Agent, AgentState.Active);
                        else
                            Messages.Message("RimMind.UI.AgentsPage.CreateFailed".Translate(),
                                MessageTypeDefOf.RejectInput, false);
                    }
                }
                return;
            }

            var agent = comp.Agent;

            // Agent state info
            string stateLabel = agent.State switch
            {
                AgentState.Active => "RimMind.UI.AgentsPage.Active".Translate(),
                AgentState.Paused => "RimMind.UI.AgentsPage.Paused".Translate(),
                AgentState.Terminated => "RimMind.UI.AgentsPage.Terminated".Translate(),
                _ => "RimMind.UI.AgentsPage.Dormant".Translate()
            };
            y = RimMindUI.DrawKeyValueRow(rect, y - rect.y,
                "RimMind.UI.AgentsPage.State".Translate(), stateLabel) + rect.y;
            y += RimMindUI.Padding;

            // Action buttons — only draw if not overlapping chat zone
            if (y + RimMindUI.BtnHeight < chatZoneTop)
            {
                // State-dependent primary action
                switch (agent.State)
                {
                    case AgentState.Active:
                        if (Widgets.ButtonText(new Rect(rect.x, y, 120f, RimMindUI.BtnHeight),
                            "RimMind.UI.AgentsPage.Pause".Translate()))
                            SafeTransitionTo(agent, AgentState.Paused);
                        break;
                    case AgentState.Paused:
                        if (Widgets.ButtonText(new Rect(rect.x, y, 120f, RimMindUI.BtnHeight),
                            "RimMind.UI.AgentsPage.Resume".Translate()))
                            SafeTransitionTo(agent, AgentState.Active);
                        break;
                    case AgentState.Dormant:
                        if (Widgets.ButtonText(new Rect(rect.x, y, 120f, RimMindUI.BtnHeight),
                            "RimMind.UI.AgentsPage.Activate".Translate()))
                            SafeTransitionTo(agent, AgentState.Active);
                        break;
                    case AgentState.Terminated:
                        if (Widgets.ButtonText(new Rect(rect.x, y, 120f, RimMindUI.BtnHeight),
                            "RimMind.UI.AgentsPage.Restart".Translate()))
                            SafeTransitionTo(agent, AgentState.Active);
                        break;
                }

                // Force Think — only meaningful for Active/Paused agents
                if (agent.State == AgentState.Active || agent.State == AgentState.Paused)
                {
                    if (Widgets.ButtonText(new Rect(rect.x + 130f, y, 120f, RimMindUI.BtnHeight),
                        "RimMind.UI.AgentsPage.ForceThink".Translate()))
                    {
                        agent.ForceThink();
                    }
                }
            }

            // Chat input at bottom — only for Active/Paused agents
            if (agent.State == AgentState.Active || agent.State == AgentState.Paused)
                DrawChat(new Rect(rect.x, chatZoneTop, rect.width, 34f), selectedPawn);
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
            var comp = CompPawnAgent.GetComp(pawn);
            if (comp?.Agent != null)
            {
                comp.Agent.ForceThink();
                Messages.Message("RimMind.UI.AgentsPage.MessageSent".Translate(),
                    MessageTypeDefOf.PositiveEvent, false);
            }
            _chatDraft = string.Empty;
        }

        private static void SafeTransitionTo(IAgentControl? agent, AgentState target)
        {
            if (agent == null) return;
            agent.TransitionTo(target);
        }
    }
}
