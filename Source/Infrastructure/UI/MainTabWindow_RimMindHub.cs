using System;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_RimMindHub : Window
    {
        private const float Padding = 8f;
        private const float TabHeight = 30f;
        private const float RowHeight = 26f;
        private const float ButtonHeight = 32f;
        private HubPage _page = HubPage.Overview;
        private int _requestPage;
        private readonly Window_RequestLog _requestLog = new Window_RequestLog();
        private readonly Window_AIDebugLog _aiDebugLog = new Window_AIDebugLog();

        public override Vector2 InitialSize => new Vector2(760f, 560f);

        public Window_RimMindHub()
        {
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "RimMind.UI.Hub.Title".Translate());

            Text.Font = GameFont.Small;
            Rect tabRect = new Rect(inRect.x, inRect.y + 38f, inRect.width, TabHeight);
            DrawTabs(tabRect);

            Rect contentRect = new Rect(inRect.x, tabRect.yMax + Padding, inRect.width,
                inRect.height - tabRect.yMax - Padding);

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
            DrawTab(rect, 0, HubPage.Overview, "RimMind.UI.Hub.Tab.Overview");
            DrawTab(rect, 1, HubPage.Requests, "RimMind.UI.Hub.Tab.Requests");
            DrawTab(rect, 2, HubPage.Agent, "RimMind.UI.Hub.Tab.Agent");
            DrawTab(rect, 3, HubPage.Debug, "RimMind.UI.Hub.Tab.Debug");
        }

        private void DrawTab(Rect rect, int index, HubPage page, string labelKey)
        {
            float width = rect.width / 4f;
            Rect tab = new Rect(rect.x + index * width, rect.y, width - 4f, rect.height);
            bool selected = _page == page;
            if (selected)
                Widgets.DrawHighlightSelected(tab);
            if (Widgets.ButtonText(tab, labelKey.Translate()))
                _page = page;
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
            DrawLine(rect, ref y, "RimMind.UI.Hub.SelectedPawn",
                selectedPawn?.LabelShortCap ?? "RimMind.UI.Hub.NoPawn".Translate());
            DrawLine(rect, ref y, "RimMind.UI.Hub.AgentSummary", agentCount, activeCount);
            DrawLine(rect, ref y, "RimMind.UI.Hub.PendingRequests", RequestOverlay.Pending.Count);
            DrawRawLine(rect, ref y, queueText);
            y += Padding;

            float colW = (rect.width - Padding) / 2f;
            DrawButton(new Rect(rect.x, y, colW, ButtonHeight), "RimMind.UI.Hub.AgentFlowLab",
                () => Find.WindowStack.Add(new Window_AgentFlowLab(selectedPawn)));
            DrawButton(new Rect(rect.x + colW + Padding, y, colW, ButtonHeight), "RimMind.UI.Hub.AgentProgress",
                () => Find.WindowStack.Add(new Window_AgentProgressFloat()));
            y += ButtonHeight + Padding;

            DrawButton(new Rect(rect.x, y, colW, ButtonHeight), "RimMind.UI.Hub.AgentState",
                () => Find.WindowStack.Add(new Window_AgentStateDebug(selectedPawn)));
            DrawButton(new Rect(rect.x + colW + Padding, y, colW, ButtonHeight), "RimMind.UI.Hub.AgentMode",
                () => Find.WindowStack.Add(new Window_AgentModeDebug(selectedPawn)));
        }

        private void DrawRequests(Rect rect)
        {
            Rect tabs = new Rect(rect.x, rect.y, rect.width, TabHeight);
            Rect pendingTab = new Rect(tabs.x, tabs.y, (tabs.width - Padding) / 2f, tabs.height);
            Rect resultsTab = new Rect(pendingTab.xMax + Padding, tabs.y, pendingTab.width, tabs.height);

            if (_requestPage == 0)
                Widgets.DrawHighlightSelected(pendingTab);
            if (_requestPage == 1)
                Widgets.DrawHighlightSelected(resultsTab);

            if (Widgets.ButtonText(pendingTab, "RimMind.UI.Hub.RequestPending".Translate()))
                _requestPage = 0;
            if (Widgets.ButtonText(resultsTab, "RimMind.UI.Hub.RequestResults".Translate()))
                _requestPage = 1;

            Rect body = new Rect(rect.x, tabs.yMax + Padding, rect.width, rect.height - tabs.height - Padding);
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
                ("RimMind.UI.Hub.ToolCallDebug", () => Find.WindowStack.Add(new Window_ToolCallDebug())),
                ("RimMind.UI.Hub.MechanismStatus", () => Find.WindowStack.Add(new Window_MechanismStatus())),
                ("RimMind.UI.Hub.ContextKeys", () => Find.WindowStack.Add(new Window_ContextKeyDebug())),
                ("RimMind.UI.Hub.AIDebugLog", () => Find.WindowStack.Add(new Window_AIDebugLog())),
                ("RimMind.UI.Hub.Settings", OpenSettings));
        }

        private static void DrawToolGrid(Rect rect, params (string LabelKey, Action Action)[] tools)
        {
            float colW = (rect.width - Padding) / 2f;
            for (int i = 0; i < tools.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                Rect button = new Rect(rect.x + col * (colW + Padding),
                    rect.y + row * (ButtonHeight + Padding), colW, ButtonHeight);
                DrawButton(button, tools[i].LabelKey, tools[i].Action);
            }
        }

        private static void DrawLine(Rect rect, ref float y, string labelKey, params object[] args)
        {
            string text = string.Format(labelKey.Translate().ToString(), args);
            Widgets.Label(new Rect(rect.x, y, rect.width, RowHeight), text);
            y += RowHeight;
        }

        private static void DrawRawLine(Rect rect, ref float y, string text)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, RowHeight), text);
            y += RowHeight;
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
