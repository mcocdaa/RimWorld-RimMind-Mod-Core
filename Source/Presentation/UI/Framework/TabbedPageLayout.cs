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
            int maxPerRow = CalculateMaxPerRow(body.width, count);
            int rows = count == 0 ? 1 : (int)System.Math.Ceiling((float)count / maxPerRow);
            int perRow = count == 0 ? 1 : (int)System.Math.Ceiling((float)count / rows);
            float idealTabBarHeight = rows * RimMindUiMetrics.TabHeight + (rows - 1) * RimMindUiMetrics.TabGap;
            float tabBarHeight = Mathf.Min(Mathf.Max(0f, idealTabBarHeight), body.height);
            float rowGap = rows <= 1 ? 0f : Mathf.Min(RimMindUiMetrics.TabGap, tabBarHeight / (rows - 1));
            float rowHeight = rows <= 0 ? 0f : Mathf.Max(0f, (tabBarHeight - rowGap * (rows - 1)) / rows);
            Rect tabBar = new Rect(body.x, body.y, body.width, tabBarHeight);
            float contentGap = Mathf.Min(RimMindUiMetrics.SectionGap, Mathf.Max(0f, body.yMax - tabBar.yMax));
            float contentY = tabBar.yMax + contentGap;
            Rect content = new Rect(
                body.x,
                contentY,
                body.width,
                Mathf.Max(0f, body.yMax - contentY));

            var tabRects = new List<TabbedPageTabRect>(count);
            for (int i = 0; i < count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                float colGap = perRow <= 1 ? 0f : Mathf.Min(RimMindUiMetrics.TabGap, body.width / (perRow - 1));
                float tabWidth = Mathf.Max(0f, (body.width - (perRow - 1) * colGap) / perRow);
                Rect tabRect = new Rect(
                    body.x + col * (tabWidth + colGap),
                    body.y + row * (rowHeight + rowGap),
                    tabWidth,
                    rowHeight);
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
