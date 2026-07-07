using UnityEngine;

namespace RimMind.Presentation.UI.Framework
{
    public sealed class TablePageLayoutResult
    {
        public TablePageLayoutResult(Rect toolbar, Rect header, Rect body, Rect bottomBar, Rect viewRect)
        {
            Toolbar = toolbar;
            Header = header;
            Body = body;
            BottomBar = bottomBar;
            ViewRect = viewRect;
        }

        public Rect Toolbar { get; }
        public Rect Header { get; }
        public Rect Body { get; }
        public Rect BottomBar { get; }
        public Rect ViewRect { get; }
    }

    public static class TablePageLayout
    {
        public static TablePageLayoutResult Calculate(Rect rect, int rowCount, int columnCount)
        {
            var split = rect.TakeBottom(RimMindUiMetrics.BottomBarHeight, RimMindUiMetrics.SectionGap);
            Rect top = split.Body;
            Rect bottom = split.Bottom;
            float toolbarHeight = Mathf.Min(RimMindUiMetrics.ButtonHeight, top.height);
            Rect toolbar = new Rect(top.x, top.y, top.width, toolbarHeight);
            float headerGap = Mathf.Min(RimMindUiMetrics.Padding, Mathf.Max(0f, top.yMax - toolbar.yMax));
            float headerY = toolbar.yMax + headerGap;
            float headerHeight = Mathf.Min(RimMindUiMetrics.DebugRowHeight, Mathf.Max(0f, top.yMax - headerY));
            Rect header = new Rect(top.x, headerY, top.width, headerHeight);
            Rect body = new Rect(
                top.x,
                header.yMax,
                top.width,
                Mathf.Max(0f, top.yMax - header.yMax));
            Rect view = new Rect(
                0f,
                0f,
                Mathf.Max(0f, Mathf.Max(rect.width - RimMindUiMetrics.ScrollBarWidth, columnCount * 120f)),
                Mathf.Max(0f, rowCount * RimMindUiMetrics.DebugRowHeight));
            return new TablePageLayoutResult(toolbar, header, body, bottom, view);
        }
    }
}
