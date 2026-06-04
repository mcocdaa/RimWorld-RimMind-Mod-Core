using System.Linq;
using RimMind.Application.Common.Interfaces.Internal;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_AIDebugLog : Window
    {
        private Vector2 _listScroll = Vector2.zero;
        private Vector2 _detailScroll = Vector2.zero;
        private string _search = string.Empty;
        private int _selectedIndex;

        public override Vector2 InitialSize => new Vector2(860f, 620f);

        public Window_AIDebugLog()
        {
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            DrawEmbedded(inRect);
        }

        public void DrawEmbedded(Rect inRect)
        {
            var debugLog = RimMindServiceLocator.TryGet<IAIDebugLog>();
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect header = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(header, "RimMind.UI.DebugLog.Title".Translate());
            Text.Font = GameFont.Small;

            Rect toolbar = new Rect(inRect.x, header.yMax + 6f, inRect.width, 30f);
            _search = Widgets.TextField(new Rect(toolbar.x, toolbar.y, toolbar.width - 92f, toolbar.height),
                _search ?? string.Empty);
            if (Widgets.ButtonText(new Rect(toolbar.xMax - 86f, toolbar.y, 86f, toolbar.height),
                    "RimMind.UI.DebugLog.Clear".Translate()))
            {
                debugLog?.Clear();
                _selectedIndex = 0;
            }

            Rect body = new Rect(inRect.x, toolbar.yMax + 8f, inRect.width,
                inRect.height - toolbar.yMax - 8f);

            if (debugLog == null)
            {
                DrawCentered(body, "RimMind.UI.DebugLog.NotReady".Translate());
                return;
            }

            var entries = debugLog.Entries
                .Reverse()
                .Where(e => string.IsNullOrWhiteSpace(_search)
                    || e.Source.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || e.FullResponse.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || e.FullUserPrompt.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (entries.Count == 0)
            {
                DrawCentered(body, "RimMind.UI.DebugLog.Empty".Translate());
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, entries.Count - 1);

            Rect listRect = new Rect(body.x, body.y, 250f, body.height);
            Rect detailRect = new Rect(listRect.xMax + 8f, body.y, body.width - listRect.width - 8f, body.height);
            DrawList(listRect, entries);
            DrawDetail(detailRect, entries[_selectedIndex]);
        }

        private void DrawList(Rect rect, System.Collections.Generic.List<AIDebugEntry> entries)
        {
            float entryH = 54f;
            Rect view = new Rect(rect.x, rect.y, rect.width - 16f, entries.Count * entryH);
            Widgets.BeginScrollView(rect, ref _listScroll, view);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                Rect row = new Rect(view.x, view.y + i * entryH, view.width, entryH - 4f);
                if (i == _selectedIndex)
                    Widgets.DrawHighlightSelected(row);
                else if (Mouse.IsOver(row))
                    Widgets.DrawHighlight(row);

                if (Widgets.ButtonInvisible(row))
                    _selectedIndex = i;

                Widgets.Label(new Rect(row.x + 4f, row.y + 4f, row.width - 8f, 22f),
                    entry.Source.NullOrEmpty() ? "RimMind.UI.Empty".Translate().ToString() : entry.Source);
                GUI.color = Color.grey;
                Widgets.Label(new Rect(row.x + 4f, row.y + 26f, row.width - 8f, 22f),
                    $"{entry.State} | {entry.TokensUsed} tok | {entry.ElapsedMs}ms");
                GUI.color = Color.white;
            }
            Widgets.EndScrollView();
        }

        private void DrawDetail(Rect rect, AIDebugEntry entry)
        {
            if (Widgets.ButtonText(new Rect(rect.xMax - 130f, rect.y, 130f, 28f),
                    "RimMind.UI.DebugLog.CopyResponse".Translate()))
            {
                GUIUtility.systemCopyBuffer = entry.FullResponse ?? string.Empty;
            }

            string text =
                $"Source: {entry.Source}\n" +
                $"Model: {entry.ModelName}\n" +
                $"Tick: {entry.GameTick} | State: {entry.State} | HTTP: {entry.HttpStatusCode}\n" +
                $"Tokens: {entry.TokensUsed} | Attempts: {entry.AttemptCount} | Queue: {entry.QueueWaitMs}ms | Processing: {entry.ProcessingMs}ms\n\n" +
                $"[System]\n{entry.FullSystemPrompt}\n\n" +
                $"[User]\n{entry.FullUserPrompt}\n\n" +
                $"[Assistant]\n{entry.FullAssistantPrompt}\n\n" +
                $"[Response]\n{entry.FullResponse}\n\n" +
                $"[Error]\n{entry.ErrorMsg}";

            Rect content = new Rect(rect.x, rect.y + 34f, rect.width - 16f,
                Text.CalcHeight(text, rect.width - 24f) + 16f);
            Widgets.BeginScrollView(new Rect(rect.x, rect.y + 34f, rect.width, rect.height - 34f),
                ref _detailScroll, content);
            Widgets.Label(new Rect(content.x, content.y, content.width, content.height), text);
            Widgets.EndScrollView();
        }

        private static void DrawCentered(Rect rect, string text)
        {
            GUI.color = Color.grey;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }
}
