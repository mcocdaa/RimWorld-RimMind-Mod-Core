using System.Collections.Generic;
using UnityEngine;

namespace RimMind.Presentation.UI.Framework
{
    public readonly struct TabbedPageTabRect
    {
        public TabbedPageTabRect(string id, Rect rect, bool selected, bool enabled)
        {
            Id = id;
            Rect = rect;
            Selected = selected;
            Enabled = enabled;
        }

        public string Id { get; }
        public Rect Rect { get; }
        public bool Selected { get; }
        public bool Enabled { get; }
    }

    public sealed class TabbedPageLayoutResult
    {
        public TabbedPageLayoutResult(Rect body, Rect tabBar, Rect content, int rowCount, IReadOnlyList<TabbedPageTabRect> tabRects)
        {
            Body = body;
            TabBar = tabBar;
            Content = content;
            RowCount = rowCount;
            TabRects = tabRects;
        }

        public Rect Body { get; }
        public Rect TabBar { get; }
        public Rect Content { get; }
        public int RowCount { get; }
        public IReadOnlyList<TabbedPageTabRect> TabRects { get; }
    }

    public static class TabbedPageLayout
    {
        public static TabbedPageLayoutResult Calculate(Rect rect, IReadOnlyList<TabbedPageTabModel> tabs)
        {
            Rect body = rect.InsetSafe(RimMindUiMetrics.WindowInset);
            int count = tabs.Count;
            int perRow = CalculateMaxPerRow(body.width, count);
            int rows = count == 0 ? 1 : (int)System.Math.Ceiling((float)count / perRow);
            float tabBarHeight = rows * RimMindUiMetrics.TabHeight + (rows - 1) * RimMindUiMetrics.TabGap;
            Rect tabBar = new Rect(body.x, body.y, body.width, tabBarHeight);
            Rect content = new Rect(
                body.x,
                tabBar.yMax + RimMindUiMetrics.SectionGap,
                body.width,
                Mathf.Max(1f, body.yMax - tabBar.yMax - RimMindUiMetrics.SectionGap));

            var tabRects = new List<TabbedPageTabRect>(count);
            for (int i = 0; i < count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                int firstIndexInRow = row * perRow;
                int colsInRow = System.Math.Min(perRow, count - firstIndexInRow);
                float tabWidth = (body.width - (colsInRow - 1) * RimMindUiMetrics.TabGap) / colsInRow;
                Rect tabRect = new Rect(
                    body.x + col * (tabWidth + RimMindUiMetrics.TabGap),
                    body.y + row * (RimMindUiMetrics.TabHeight + RimMindUiMetrics.TabGap),
                    tabWidth,
                    RimMindUiMetrics.TabHeight);
                TabbedPageTabModel tab = tabs[i];
                tabRects.Add(new TabbedPageTabRect(tab.Id, tabRect, tab.Selected, tab.Enabled));
            }

            return new TabbedPageLayoutResult(body, tabBar, content, rows, tabRects);
        }

        private static int CalculateMaxPerRow(float availableWidth, int tabCount)
        {
            if (tabCount <= 0)
                return 1;

            int fit = (int)System.Math.Floor((availableWidth + RimMindUiMetrics.TabGap) / (RimMindUiMetrics.TabMinWidth + RimMindUiMetrics.TabGap));
            return Clamp(fit, 1, tabCount);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
