using System.Collections.Generic;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.Framework
{
    public sealed class RimMindTableDrawer
    {
        private const int DebugTableColumnCount = 8;
        private const float StatusStripWidth = 4f;
        private const float CellPadding = 6f;

        private static readonly string[] DebugTableHeaderKeys =
        {
            "RimMind.UI.DebugTable.Header.Id",
            "RimMind.UI.DebugTable.Header.Status",
            "RimMind.UI.DebugTable.Header.Time",
            "RimMind.UI.DebugTable.Header.Scope",
            "RimMind.UI.DebugTable.Header.Actor",
            "RimMind.UI.DebugTable.Header.Channel",
            "RimMind.UI.DebugTable.Header.Model",
            "RimMind.UI.DebugTable.Header.Summary"
        };

        public void Draw(Rect rect, DebugTableModel model, ref Vector2 scroll, RimMindLayoutScope scope)
        {
            TablePageLayoutResult layout = TablePageLayout.Calculate(rect, model.Rows.Count, columnCount: DebugTableColumnCount);
            scope.Record(layout.Toolbar, "Table:Toolbar");
            scope.Record(layout.Header, "Table:Header");
            scope.Record(layout.Body, "Table:Body");
            scope.Record(layout.BottomBar, "Table:BottomBar");

            DrawToolbar(layout.Toolbar, model.Title);
            DrawDebugHeaders(layout);
            DrawDebugRows(layout, model.Rows, ref scroll);
        }

        public void Draw(
            TablePageLayoutResult layout,
            IReadOnlyList<string> headers,
            IReadOnlyList<IReadOnlyList<string>> rows,
            ref Vector2 scroll,
            RimMindLayoutScope scope)
        {
            scope.Record(layout.Toolbar, "Table:Toolbar");
            scope.Record(layout.Header, "Table:Header");
            scope.Record(layout.Body, "Table:Body");
            scope.Record(layout.BottomBar, "Table:BottomBar");

            float colWidth = layout.ViewRect.width / Mathf.Max(1, headers.Count);
            for (int c = 0; c < headers.Count; c++)
            {
                Rect headerRect = new Rect(layout.Header.x + c * colWidth, layout.Header.y, colWidth, layout.Header.height);
                Widgets.Label(headerRect, headers[c]);
            }

            Widgets.BeginScrollView(layout.Body, ref scroll, layout.ViewRect);
            for (int r = 0; r < rows.Count; r++)
            {
                Rect rowRect = new Rect(0f, r * RimMindUiMetrics.DebugRowHeight, layout.ViewRect.width, RimMindUiMetrics.DebugRowHeight);
                if (r % 2 == 0)
                    Widgets.DrawBoxSolid(rowRect, RimMindUI.ColorSectionBg);
                for (int c = 0; c < rows[r].Count; c++)
                {
                    Widgets.Label(new Rect(c * colWidth, rowRect.y, colWidth, rowRect.height), rows[r][c]);
                }
            }

            Widgets.EndScrollView();
        }

        private static void DrawToolbar(Rect rect, string title)
        {
            GUI.color = RimMindUI.ColorHeader;
            Widgets.Label(rect, title);
            GUI.color = Color.white;
        }

        private static void DrawDebugHeaders(TablePageLayoutResult layout)
        {
            float colWidth = layout.Header.width / DebugTableColumnCount;
            GUI.color = RimMindUI.ColorKey;
            for (int c = 0; c < DebugTableHeaderKeys.Length; c++)
            {
                Rect headerRect = new Rect(layout.Header.x + c * colWidth + CellPadding, layout.Header.y, colWidth - CellPadding, layout.Header.height);
                Widgets.Label(headerRect, DebugTableHeaderKeys[c].Translate());
            }

            GUI.color = Color.white;
        }

        private static void DrawDebugRows(TablePageLayoutResult layout, IReadOnlyList<DebugTableRow> rows, ref Vector2 scroll)
        {
            float colWidth = layout.ViewRect.width / DebugTableColumnCount;
            Widgets.BeginScrollView(layout.Body, ref scroll, layout.ViewRect);
            for (int r = 0; r < rows.Count; r++)
            {
                DebugTableRow row = rows[r];
                Rect rowRect = new Rect(0f, r * RimMindUiMetrics.DebugRowHeight, layout.ViewRect.width, RimMindUiMetrics.DebugRowHeight);
                if (r % 2 == 0)
                    Widgets.DrawBoxSolid(rowRect, RimMindUI.ColorSectionBg);

                Widgets.DrawBoxSolid(new Rect(rowRect.x, rowRect.y, StatusStripWidth, rowRect.height), ColorFor(row.StatusColorName));
                DrawDebugCells(rowRect, colWidth, row);
            }

            Widgets.EndScrollView();
        }

        private static void DrawDebugCells(Rect rowRect, float colWidth, DebugTableRow row)
        {
            string[] cells =
            {
                row.Id,
                StatusLabelFor(row.Status),
                row.Time,
                row.Scope,
                row.Actor,
                row.Channel,
                row.Model,
                string.IsNullOrWhiteSpace(row.Duration) ? row.Summary : row.Summary + " / " + row.Duration
            };

            GUI.color = RimMindUI.ColorValue;
            for (int c = 0; c < cells.Length; c++)
            {
                float x = c * colWidth + CellPadding;
                if (c == 0)
                    x += StatusStripWidth;
                Rect cell = new Rect(x, rowRect.y, colWidth - CellPadding, rowRect.height);
                Widgets.Label(cell, cells[c] ?? string.Empty);
            }

            GUI.color = Color.white;
        }

        private static string StatusLabelFor(DebugTableStatus status)
        {
            string key = status switch
            {
                DebugTableStatus.Waiting => "RimMind.UI.DebugTable.Status.Waiting",
                DebugTableStatus.Streaming => "RimMind.UI.DebugTable.Status.Streaming",
                DebugTableStatus.Completed => "RimMind.UI.DebugTable.Status.Completed",
                DebugTableStatus.Failed => "RimMind.UI.DebugTable.Status.Failed",
                DebugTableStatus.Cancelled => "RimMind.UI.DebugTable.Status.Cancelled",
                _ => "RimMind.UI.DebugTable.Status.Cancelled"
            };

            return key.Translate();
        }

        private static Color ColorFor(string colorName)
        {
            return (colorName ?? string.Empty).ToLowerInvariant() switch
            {
                "orange" => new Color(1f, 0.65f, 0.2f),
                "blue" => new Color(0.35f, 0.65f, 1f),
                "green" => RimMindUI.ColorActive,
                "red" => RimMindUI.ColorError,
                "gray" => RimMindUI.ColorMuted,
                _ => RimMindUI.ColorMuted
            };
        }
    }
}
