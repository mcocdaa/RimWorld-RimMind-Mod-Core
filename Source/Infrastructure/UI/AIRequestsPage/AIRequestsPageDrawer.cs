using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Debug;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.AIRequestsPage
{
    public sealed class AIRequestsPageDrawer
    {
        private int _selectedIndex;
        private Vector2 _scrollPosition;
        private Vector2 _detailScrollPosition;
        private const float RowHeight = 48f;
        private const float RowContentHeight = 44f;
        private const int RowPreviewChars = 120;

        public void Draw(Rect rect)
        {
            var log = RimMindServiceLocator.TryGet<IAIRequestTraceLog>();
            if (log == null)
            {
                Widgets.Label(rect, "RimMind.UI.AIRequestsPage.TraceUnavailable".Translate());
                return;
            }

            IReadOnlyList<AIRequestTraceEntry> entries = log.Entries;

            if (entries.Count == 0)
            {
                Widgets.Label(rect, "RimMind.UI.AIRequestsPage.Empty".Translate());
                return;
            }

            float listWidth = Mathf.Min(300f, rect.width * 0.4f);
            Rect list = new(rect.x, rect.y, listWidth, rect.height);
            Rect detail = new(list.xMax + 8f, rect.y, rect.width - listWidth - 8f, rect.height);
            DrawList(list, entries);
            DrawDetail(detail, entries[Mathf.Clamp(_selectedIndex, 0, entries.Count - 1)]);
        }

        private void DrawList(Rect rect, IReadOnlyList<AIRequestTraceEntry> entries)
        {
            float contentHeight = entries.Count * RowHeight;
            Rect viewRect = new(rect.x, rect.y, rect.width - (contentHeight > rect.height ? 16f : 0f), contentHeight);

            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                Rect row = new(viewRect.x, viewRect.y + i * RowHeight, viewRect.width, RowContentHeight);

                if (i == _selectedIndex)
                    Widgets.DrawHighlight(row);

                Widgets.DrawBoxSolid(new Rect(row.x, row.y, 4f, row.height), ColorFor(entry.State));
                if (Widgets.ButtonInvisible(row))
                    _selectedIndex = i;
                Widgets.Label(new Rect(row.x + 8f, row.y + 2f, row.width - 12f, 22f), entry.Source);
                Widgets.Label(new Rect(row.x + 8f, row.y + 22f, row.width - 12f, 22f), TruncateForRow(entry.UserPrompt));
                if (entry.State == AIRequestTraceState.Failed && !string.IsNullOrWhiteSpace(entry.Error))
                    TooltipHandler.TipRegion(row, entry.Error);
            }
            Widgets.EndScrollView();
        }

        private void DrawDetail(Rect rect, AIRequestTraceEntry entry)
        {
            float viewHeight = Mathf.Max(rect.height + 1f, 1200f);
            Rect view = new(rect.x, rect.y, rect.width - 16f, viewHeight);
            Widgets.BeginScrollView(rect, ref _detailScrollPosition, view);

            float y = view.y;
            y = DrawSection(view, y, "RimMind.UI.AIRequestsPage.Detail.Meta".Translate(),
                $"{entry.RequestId}\n{StateLabelFor(entry.State)}\n{entry.Source}\n{entry.Model}\n{entry.ElapsedMs} ms\n{entry.TokensUsed} tokens");
            y = DrawSection(view, y, "RimMind.UI.AIRequestsPage.Detail.System".Translate(), entry.SystemPrompt);
            y = DrawSection(view, y, "RimMind.UI.AIRequestsPage.Detail.User".Translate(), entry.UserPrompt);
            y = DrawSection(view, y, "RimMind.UI.AIRequestsPage.Detail.Assistant".Translate(), entry.AssistantPrompt);
            y = DrawSection(view, y, "RimMind.UI.AIRequestsPage.Detail.Response".Translate(), entry.Response);
            y = DrawSection(view, y, "RimMind.UI.AIRequestsPage.Detail.Error".Translate(), entry.Error ?? string.Empty);
            DrawSection(view, y, "RimMind.UI.AIRequestsPage.Detail.ToolCalls".Translate(), FormatToolCalls(entry));

            Widgets.EndScrollView();
        }

        private static float DrawSection(Rect view, float y, string title, string body)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(view.x, y, view.width, 24f), title);
            y += 24f;

            string text = string.IsNullOrWhiteSpace(body)
                ? "RimMind.UI.AIRequestsPage.Detail.EmptySection".Translate()
                : body;
            float height = Mathf.Max(32f, Text.CalcHeight(text, view.width));
            Widgets.Label(new Rect(view.x, y, view.width, height), text);
            return y + height + 12f;
        }

        private static string FormatToolCalls(AIRequestTraceEntry entry)
        {
            if (entry.ToolCalls.Count == 0)
                return string.Empty;

            return string.Join("\n", entry.ToolCalls.Select(t =>
                $"{t.ToolName} [{(t.Succeeded ? "ok" : "error")}] {t.Error ?? string.Empty}"));
        }

        private static string TruncateForRow(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string oneLine = value.Replace("\r", " ").Replace("\n", " ");
            return oneLine.Length <= RowPreviewChars
                ? oneLine
                : oneLine.Substring(0, RowPreviewChars) + "...";
        }

        private static string StateLabelFor(AIRequestTraceState state)
            => state switch
            {
                AIRequestTraceState.Running => "RimMind.UI.AIRequestsPage.State.Running".Translate(),
                AIRequestTraceState.Completed => "RimMind.UI.AIRequestsPage.State.Completed".Translate(),
                AIRequestTraceState.Failed => "RimMind.UI.AIRequestsPage.State.Failed".Translate(),
                _ => state.ToString()
            };

        private static Color ColorFor(AIRequestTraceState state)
            => state switch
            {
                AIRequestTraceState.Running => new Color(1f, 0.65f, 0.2f),
                AIRequestTraceState.Completed => new Color(0.4f, 1f, 0.4f),
                AIRequestTraceState.Failed => new Color(1f, 0.25f, 0.25f),
                _ => Color.white
            };
    }
}
