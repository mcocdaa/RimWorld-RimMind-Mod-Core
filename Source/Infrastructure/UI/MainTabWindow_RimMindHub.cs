using System;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation;
using RimMind.Presentation.UI;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_RimMindHub : Window
    {
        private HubPage _page;
        private int _requestPage;
        private readonly Window_RequestLog _requestLog = new Window_RequestLog();
        private readonly Window_AIDebugLog _aiDebugLog = new Window_AIDebugLog();

        public override Vector2 InitialSize => new Vector2(780f, 580f);

        public Window_RimMindHub()
            : this(HubPage.Requests, 1)
        {
        }

        private Window_RimMindHub(HubPage initialPage, int requestPage)
        {
            _page = initialPage;
            _requestPage = requestPage;
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = RimMindUI.DrawWindowHeader(inRect, "RimMind.UI.Hub.Title".Translate());

            // Tab bar
            Rect tabRect = new Rect(inRect.x, y, inRect.width, RimMindUI.TabHeight);
            DrawTabs(tabRect);
            y = tabRect.yMax + RimMindUI.Padding;

            Rect contentRect = new Rect(inRect.x, y, inRect.width, inRect.height - y + inRect.y);

            switch (_page)
            {
                case HubPage.Overview:
                    DrawOverview(contentRect);
                    break;
                case HubPage.Requests:
                    DrawRequests(contentRect);
                    break;
                case HubPage.Agent:
                    DrawAgentTools(contentRect);
                    break;
                case HubPage.Debug:
                    DrawDebugTools(contentRect);
                    break;
            }
        }

        private void DrawTabs(Rect rect)
        {
            float tabW = rect.width / 4f;
            var pages = new[] { HubPage.Overview, HubPage.Requests, HubPage.Agent, HubPage.Debug };
            var labels = new[] { "RimMind.UI.Hub.Tab.Overview", "RimMind.UI.Hub.Tab.Requests", "RimMind.UI.Hub.Tab.Agent", "RimMind.UI.Hub.Tab.Debug" };

            for (int i = 0; i < 4; i++)
            {
                Rect tabBtn = new Rect(rect.x + i * tabW, rect.y, tabW - 2f, rect.height);
                bool selected = _page == pages[i];
                if (RimMindUI.DrawTabButton(tabBtn, labels[i].Translate(), selected))
                    _page = pages[i];
            }
        }

        private static void DrawOverview(Rect rect)
        {
            Pawn? selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
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

            // ── Status Cards ──
            float cardW = (rect.width - RimMindUI.Padding) / 2f;
            float cardH = 60f;

            // Agent card
            Rect agentCard = new Rect(rect.x, y, cardW, cardH);
            Widgets.DrawBoxSolid(agentCard, RimMindUI.ColorCardBg);
            float innerY = agentCard.y + RimMindUI.Padding;
            GUI.color = RimMindUI.ColorSectionTitle;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(agentCard.x + RimMindUI.Padding, innerY, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                "RimMind.UI.Hub.AgentSummary".Translate());
            Text.Font = GameFont.Medium;
            GUI.color = activeCount > 0 ? RimMindUI.ColorActive : RimMindUI.ColorMuted;
            Widgets.Label(new Rect(agentCard.x + RimMindUI.Padding, innerY + RimMindUI.LineHeight, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                $"{activeCount} / {agentCount}");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // Queue card
            Rect queueCard = new Rect(rect.x + cardW + RimMindUI.Padding, y, cardW, cardH);
            Widgets.DrawBoxSolid(queueCard, RimMindUI.ColorCardBg);
            innerY = queueCard.y + RimMindUI.Padding;
            GUI.color = RimMindUI.ColorSectionTitle;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(queueCard.x + RimMindUI.Padding, innerY, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                "RimMind.UI.Hub.QueueState".Translate());
            Text.Font = GameFont.Medium;
            bool isQueueRunning = queue != null && !queue.IsPaused;
            GUI.color = isQueueRunning ? RimMindUI.ColorActive : RimMindUI.ColorPaused;
            Widgets.Label(new Rect(queueCard.x + RimMindUI.Padding, innerY + RimMindUI.LineHeight, cardW - RimMindUI.Padding * 2, RimMindUI.LineHeight),
                queueText);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            y += cardH + RimMindUI.SectionGap;

            // ── Selected Pawn Info ──
            y = RimMindUI.DrawSectionHeader(rect, y - rect.y, "RimMind.UI.Hub.SelectedPawn".Translate()) + rect.y;
            string pawnText = selectedPawn?.LabelShortCap ?? "RimMind.UI.Hub.NoPawn".Translate();
            y = RimMindUI.DrawKeyValueRow(rect, y - rect.y, "RimMind.UI.Hub.SelectedPawn".Translate(), pawnText) + rect.y;
            y = RimMindUI.DrawKeyValueRow(rect, y - rect.y, "RimMind.UI.Hub.PendingRequests".Translate(), RequestOverlay.Pending.Count.ToString()) + rect.y;

            y += RimMindUI.SectionGap;

            // ── Quick Actions ──
            y = RimMindUI.DrawSectionHeader(rect, y - rect.y, "RimMind.UI.Hub.QuickActions".Translate()) + rect.y;

            float colW = (rect.width - RimMindUI.Padding) / 2f;
            DrawButton(new Rect(rect.x, y, colW, RimMindUI.BtnHeight), "RimMind.UI.Hub.AgentFlowLab",
                () => Find.WindowStack.Add(new Window_AgentFlowLab(selectedPawn)));
            DrawButton(new Rect(rect.x + colW + RimMindUI.Padding, y, colW, RimMindUI.BtnHeight), "RimMind.UI.Hub.AgentProgress",
                () => Find.WindowStack.Add(new Window_AgentProgressFloat()));
            y += RimMindUI.BtnHeight + RimMindUI.Padding;

            DrawButton(new Rect(rect.x, y, colW, RimMindUI.BtnHeight), "RimMind.UI.Hub.AgentState",
                () => Find.WindowStack.Add(new Window_AgentStateDebug(selectedPawn)));
            DrawButton(new Rect(rect.x + colW + RimMindUI.Padding, y, colW, RimMindUI.BtnHeight), "RimMind.UI.Hub.AgentMode",
                () => Find.WindowStack.Add(new Window_AgentModeDebug(selectedPawn)));
        }

        private void DrawRequests(Rect rect)
        {
            Rect tabs = new Rect(rect.x, rect.y, rect.width, RimMindUI.TabHeight);
            float halfW = (tabs.width - RimMindUI.Padding) / 2f;
            Rect pendingTab = new Rect(tabs.x, tabs.y, halfW, tabs.height);
            Rect resultsTab = new Rect(pendingTab.xMax + RimMindUI.Padding, tabs.y, halfW, tabs.height);

            if (RimMindUI.DrawTabButton(pendingTab, "RimMind.UI.Hub.RequestPending".Translate(), _requestPage == 0))
                _requestPage = 0;
            if (RimMindUI.DrawTabButton(resultsTab, "RimMind.UI.Hub.RequestResults".Translate(), _requestPage == 1))
                _requestPage = 1;

            Rect body = new Rect(rect.x, tabs.yMax + RimMindUI.Padding, rect.width, rect.height - tabs.height - RimMindUI.Padding);
            if (_requestPage == 0)
                _requestLog.DrawEmbedded(body);
            else
                _aiDebugLog.DrawEmbedded(body);
        }

        private static void DrawAgentTools(Rect rect)
        {
            Pawn? selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            DrawToolGrid(rect,
                ("RimMind.UI.Hub.AgentFlowLab", () => Find.WindowStack.Add(new Window_AgentFlowLab(selectedPawn))),
                ("RimMind.UI.Hub.AgentState", () => Find.WindowStack.Add(new Window_AgentStateDebug(selectedPawn))),
                ("RimMind.UI.Hub.AgentMode", () => Find.WindowStack.Add(new Window_AgentModeDebug(selectedPawn))),
                ("RimMind.UI.Hub.AgentProgress", () => Find.WindowStack.Add(new Window_AgentProgressFloat())));
        }

        private static void DrawDebugTools(Rect rect)
        {
            DrawToolGrid(rect,
                ("RimMind.UI.Hub.RequestLog", () => Find.WindowStack.Add(new Window_RequestLog())),
                ("RimMind.UI.Hub.AIDebugLog", () => Find.WindowStack.Add(new Window_AIDebugLog())),
                ("RimMind.UI.Hub.ToolCallDebug", () => Find.WindowStack.Add(new Window_ToolCallDebug())),
                ("RimMind.UI.Hub.MechanismStatus", () => Find.WindowStack.Add(new Window_MechanismStatus())),
                ("RimMind.UI.Hub.ContextKeys", () => Find.WindowStack.Add(new Window_ContextKeyDebug())),
                ("RimMind.UI.Hub.Settings", OpenSettings));
        }

        private static void DrawToolGrid(Rect rect, params (string LabelKey, Action Action)[] tools)
        {
            float colW = (rect.width - RimMindUI.Padding) / 2f;
            for (int i = 0; i < tools.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                Rect button = new Rect(rect.x + col * (colW + RimMindUI.Padding),
                    rect.y + row * (RimMindUI.BtnHeight + RimMindUI.Padding), colW, RimMindUI.BtnHeight);
                DrawButton(button, tools[i].LabelKey, tools[i].Action);
            }
        }

        private static void DrawButton(Rect rect, string labelKey, Action action)
        {
            if (Widgets.ButtonText(rect, labelKey.Translate()))
                action();
        }

        private static void OpenSettings()
        {
            var sp = RimMindServiceLocator.TryGet<ISettingsProvider>();
            if (sp != null)
                Find.WindowStack.Add(new Window_RimMindSettings(sp));
        }

        private enum HubPage
        {
            Overview,
            Requests,
            Agent,
            Debug
        }
    }

    public class MainTabWindow_RimMindHub : Window_RimMindHub
    {
    }
}
