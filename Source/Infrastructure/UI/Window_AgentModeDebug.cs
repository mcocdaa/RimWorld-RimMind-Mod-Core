using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Events;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_AgentModeDebug : Window
    {
        private Vector2 _scrollPos = Vector2.zero;
        private Vector2 _historyScrollPos = Vector2.zero;
        private Vector2 _modesScrollPos = Vector2.zero;
        private const float Padding = 6f;
        private const float LineH = 22f;
        private const float BtnHeight = 24f;
        private const int MaxHistoryEntries = 20;

        private int _selectedPawnIndex = -1;
        private int _targetModeIndex;
        private bool _isSubscribed;
        private readonly List<AgentModeChangedEvent> _modeChangeHistory = new();
        private List<Pawn> _cachedPawns = new();
        private Pawn? _initialPawn;

        public override Vector2 InitialSize => new Vector2(720f, 560f);

        public Window_AgentModeDebug() : this(null) { }

        public Window_AgentModeDebug(Pawn? pawn)
        {
            _initialPawn = pawn;
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            EnsureSubscribed();
            RefreshPawnCache();

            float headerH = 30f;
            float pawnListH = 100f;
            float detailH = 180f;
            float modesHeaderH = LineH + Padding;
            float modesH = 60f;
            float historyHeaderH = LineH + Padding;
            float historyH = inRect.height - headerH - pawnListH - detailH - modesHeaderH - modesH - historyHeaderH - Padding * 7;

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerH);
            float y = headerRect.yMax + Padding;
            Rect pawnListRect = new Rect(inRect.x, y, inRect.width, pawnListH);
            y = pawnListRect.yMax + Padding;
            Rect detailRect = new Rect(inRect.x, y, inRect.width, detailH);
            y = detailRect.yMax + Padding;
            Rect modesHeaderRect = new Rect(inRect.x, y, inRect.width, modesHeaderH);
            y = modesHeaderRect.yMax + Padding;
            Rect modesRect = new Rect(inRect.x, y, inRect.width, modesH);
            y = modesRect.yMax + Padding;
            Rect historyHeaderRect = new Rect(inRect.x, y, inRect.width, historyHeaderH);
            y = historyHeaderRect.yMax + Padding;
            Rect historyRect = new Rect(inRect.x, y, inRect.width, Mathf.Max(historyH, 40f));

            DrawHeader(headerRect);
            DrawPawnList(pawnListRect);
            DrawSelectedPawnDetail(detailRect);
            DrawRegisteredModesHeader(modesHeaderRect);
            DrawRegisteredModes(modesRect);
            DrawHistoryHeader(historyHeaderRect);
            DrawHistory(historyRect);
        }

        public override void PreClose()
        {
            Unsubscribe();
            base.PreClose();
        }

        private void EnsureSubscribed()
        {
            if (_isSubscribed) return;

            var bus = RimMindServiceLocator.Get<IAgentBus>();
            if (bus == null) return;

            bus.Subscribe<AgentModeChangedEvent>("AgentModeDebugWindow", OnModeChanged);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;

            var bus = RimMindServiceLocator.Get<IAgentBus>();
            if (bus == null) return;

            bus.Unsubscribe<AgentModeChangedEvent>("AgentModeDebugWindow");
            _isSubscribed = false;
        }

        private void OnModeChanged(AgentModeChangedEvent evt)
        {
            _modeChangeHistory.Add(evt);
            while (_modeChangeHistory.Count > MaxHistoryEntries)
                _modeChangeHistory.RemoveAt(0);
        }

        private void RefreshPawnCache()
        {
            _cachedPawns.Clear();
            var map = Find.CurrentMap;
            if (map == null) return;

            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                var comp = CompPawnAgent.GetComp(pawn);
                if (comp?.Agent != null)
                    _cachedPawns.Add(pawn);
            }

            if (_initialPawn != null && _selectedPawnIndex < 0)
            {
                int idx = _cachedPawns.IndexOf(_initialPawn);
                if (idx >= 0)
                    _selectedPawnIndex = idx;
                _initialPawn = null;
            }
        }

        private void DrawHeader(Rect rect)
        {
            GUI.color = new Color(0.7f, 0.8f, 1f);
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "RimMind.UI.AgentModeDebug.Title".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawPawnList(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.16f, 0.5f));

            if (_cachedPawns.Count == 0)
            {
                float centerY = rect.y + rect.height / 2f;

                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(rect.x, centerY - 20f, rect.width, LineH),
                    "RimMind.UI.AgentModeDebug.NoPawns".Translate());

                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                string hint = "RimMind.UI.AgentModeDebug.NoPawnsHint".Translate();
                float hintH = Text.CalcHeight(hint, rect.width - 24f);
                Widgets.Label(new Rect(rect.x + 12f, centerY + 2f, rect.width - 24f, hintH), hint);
                Text.Font = GameFont.Small;

                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            float contentH = _cachedPawns.Count * LineH;
            Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, contentH);
            Widgets.BeginScrollView(rect, ref _scrollPos, viewRect);

            float y = rect.y;
            for (int i = 0; i < _cachedPawns.Count; i++)
            {
                Pawn pawn = _cachedPawns[i];
                var comp = CompPawnAgent.GetComp(pawn);
                if (comp?.Agent == null) continue;

                IAgentControl agent = comp.Agent;
                string label = $"[{pawn.Name?.ToStringShort ?? pawn.LabelShort}] " +
                    "RimMind.UI.AgentModeDebug.Mode".Translate((string)agent.CurrentModeId);

                Rect rowRect = new Rect(viewRect.x, y, viewRect.width, LineH);
                if (i == _selectedPawnIndex)
                    Widgets.DrawBoxSolid(rowRect, new Color(0.3f, 0.4f, 0.6f, 0.5f));

                if (Widgets.ButtonInvisible(rowRect))
                    _selectedPawnIndex = i;

                GUI.color = i == _selectedPawnIndex ? new Color(0.85f, 0.9f, 1f) : Color.white;
                Widgets.Label(new Rect(rowRect.x + Padding, rowRect.y, rowRect.width - Padding * 2, LineH), label);
                GUI.color = Color.white;

                y += LineH;
            }

            Widgets.EndScrollView();
        }

        private void DrawSelectedPawnDetail(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.14f, 0.5f));

            if (_selectedPawnIndex < 0 || _selectedPawnIndex >= _cachedPawns.Count)
            {
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimMind.UI.AgentModeDebug.SelectPawn".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            Pawn pawn = _cachedPawns[_selectedPawnIndex];
            var comp = CompPawnAgent.GetComp(pawn);
            if (comp?.Agent == null) return;

            IAgentControl agent = comp.Agent;
            IAgentMode currentMode = agent.CurrentMode;
            float x = rect.x + Padding;
            float y = rect.y + Padding;
            float labelW = rect.width - Padding * 2;

            // Pawn name + mode
            string nameLine = $"[{pawn.Name?.ToStringShort ?? pawn.LabelShort}] " +
                "RimMind.UI.AgentModeDebug.Mode".Translate((string)agent.CurrentModeId);
            GUI.color = new Color(0.85f, 0.9f, 1f);
            Widgets.Label(new Rect(x, y, labelW, LineH), nameLine);
            GUI.color = Color.white;
            y += LineH + Padding;

            // IsActive
            string activeLabel = agent.IsActive
                ? "RimMind.UI.AgentModeDebug.Active".Translate()
                : "RimMind.UI.AgentModeDebug.Inactive".Translate();
            GUI.color = agent.IsActive ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.4f);
            Widgets.Label(new Rect(x, y, labelW, LineH), activeLabel);
            GUI.color = Color.white;
            y += LineH + Padding;

            // AllowedToolIds
            if (currentMode != null)
            {
                var toolRegistry = RimMindAPI.Tools;
                IReadOnlyList<string> allowedTools = toolRegistry != null
                    ? currentMode.AllowedToolIds(toolRegistry)
                    : Array.Empty<string>();
                string toolsStr = allowedTools.Count > 0 ? string.Join(", ", allowedTools) : "-";
                string toolsLabel = "RimMind.UI.AgentModeDebug.AllowedTools".Translate(toolsStr);
                float toolsH = Text.CalcHeight(toolsLabel, labelW);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(x, y, labelW, toolsH), toolsLabel);
                GUI.color = Color.white;
                y += toolsH + Padding;

                // ShouldThink
                bool shouldThink = currentMode.ShouldThink(agent, Array.Empty<PerceptionBufferEntry>());
                string thinkLabel = "RimMind.UI.AgentModeDebug.ShouldThink".Translate(shouldThink.ToString());
                GUI.color = shouldThink ? new Color(0.4f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(x, y, labelW, LineH), thinkLabel);
                GUI.color = Color.white;
                y += LineH + Padding;
            }

            // Mode switch controls
            DrawModeSwitchControls(new Rect(x, y, labelW, BtnHeight + LineH + Padding), agent);
        }

        private void DrawModeSwitchControls(Rect rect, IAgentControl agent)
        {
            IExtensionRegistry<IAgentMode>? modeRegistry = RimMindAPI.Modes;
            if (modeRegistry == null) return;

            IReadOnlyList<IAgentMode> modes = modeRegistry.All;
            if (modes.Count == 0) return;

            // Clamp target mode index
            if (_targetModeIndex < 0 || _targetModeIndex >= modes.Count)
                _targetModeIndex = 0;

            float x = rect.x;
            float y = rect.y;

            // "Switch to:" label
            string switchToLabel = "RimMind.UI.AgentModeDebug.SwitchTo".Translate();
            float switchToW = Text.CalcSize(switchToLabel).x + Padding;
            Widgets.Label(new Rect(x, y, switchToW, LineH), switchToLabel);
            x += switchToW;

            // Mode dropdown buttons
            for (int i = 0; i < modes.Count; i++)
            {
                string modeLabel = (string)modes[i].ModeId;
                float btnW = Text.CalcSize(modeLabel).x + Padding * 4;
                Rect btnRect = new Rect(x, y, btnW, BtnHeight);

                if (i == _targetModeIndex)
                    Widgets.DrawBoxSolid(btnRect, new Color(0.3f, 0.5f, 0.7f, 0.6f));

                if (Widgets.ButtonText(btnRect, modeLabel))
                    _targetModeIndex = i;

                x += btnW + Padding;

                if (x + 60f > rect.xMax)
                    break;
            }

            y += BtnHeight + Padding;

            // Switch button
            Rect switchBtnRect = new Rect(rect.x, y, 120f, BtnHeight);
            if (Widgets.ButtonText(switchBtnRect, "RimMind.UI.AgentModeDebug.SwitchMode".Translate()))
            {
                if (_targetModeIndex >= 0 && _targetModeIndex < modes.Count)
                {
                    agent.SwitchMode(modes[_targetModeIndex].ModeId);
                }
            }
        }

        private void DrawHistoryHeader(Rect rect)
        {
            GUI.color = new Color(0.7f, 0.8f, 1f);
            Text.Font = GameFont.Small;
            Widgets.Label(rect, "RimMind.UI.AgentModeDebug.History".Translate());
            GUI.color = Color.white;
        }

        private void DrawRegisteredModesHeader(Rect rect)
        {
            GUI.color = new Color(0.7f, 0.8f, 1f);
            Text.Font = GameFont.Small;
            Widgets.Label(rect, "RimMind.UI.AgentModeDebug.RegisteredModes".Translate());
            GUI.color = Color.white;
        }

        private void DrawRegisteredModes(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.16f, 0.5f));

            IExtensionRegistry<IAgentMode>? modeRegistry = RimMindAPI.Modes;
            if (modeRegistry == null)
            {
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimMind.UI.AgentModeDebug.NoModes".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            IReadOnlyList<IAgentMode> modes = modeRegistry.All;
            if (modes.Count == 0)
            {
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimMind.UI.AgentModeDebug.NoModes".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            float contentH = modes.Count * LineH;
            Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, contentH);
            Widgets.BeginScrollView(rect, ref _modesScrollPos, viewRect);

            float y = rect.y;
            for (int i = 0; i < modes.Count; i++)
            {
                IAgentMode mode = modes[i];
                string entry = "RimMind.UI.AgentModeDebug.ModeEntry".Translate(
                    (string)mode.ModeId, mode.DisplayName);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(viewRect.x + Padding, y, viewRect.width - Padding * 2, LineH), entry);
                GUI.color = Color.white;
                y += LineH;
            }

            Widgets.EndScrollView();
        }

        private void DrawHistory(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.16f, 0.5f));

            if (_modeChangeHistory.Count == 0)
            {
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimMind.UI.AgentModeDebug.NoHistory".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            float contentH = _modeChangeHistory.Count * LineH;
            Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, contentH);
            Widgets.BeginScrollView(rect, ref _historyScrollPos, viewRect);

            float y = rect.y;
            for (int i = _modeChangeHistory.Count - 1; i >= 0; i--)
            {
                AgentModeChangedEvent evt = _modeChangeHistory[i];
                string tickInfo = $" [T:{evt.Timestamp}]";
                string entry = "RimMind.UI.AgentModeDebug.ModeChange".Translate(
                    evt.NpcId, evt.OldMode, evt.NewMode) + tickInfo;

                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(viewRect.x + Padding, y, viewRect.width - Padding * 2, LineH),
                    entry);
                GUI.color = Color.white;
                y += LineH;
            }

            Widgets.EndScrollView();
        }
    }
}
