using System.Collections.Generic;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.Framework
{
    public sealed class RimMindTableDrawer
    {
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
    }
}
