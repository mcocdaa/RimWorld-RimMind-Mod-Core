using RimMind.Presentation.UI.Framework;
using UnityEngine;

namespace RimMind.Infrastructure.UI.DebugCenter
{
    public readonly struct HubLayoutRects
    {
        public HubLayoutRects(Rect body, Rect header, Rect tabs, Rect content)
        {
            Body = body;
            Header = header;
            Tabs = tabs;
            Content = content;
        }

        public Rect Body { get; }
        public Rect Header { get; }
        public Rect Tabs { get; }
        public Rect Content { get; }
    }

    public readonly struct AgentPageLayoutRects
    {
        public AgentPageLayoutRects(Rect list, Rect detail, Rect header, Rect actions, Rect activity, Rect chat)
        {
            List = list;
            Detail = detail;
            Header = header;
            Actions = actions;
            Activity = activity;
            Chat = chat;
        }

        public Rect List { get; }
        public Rect Detail { get; }
        public Rect Header { get; }
        public Rect Actions { get; }
        public Rect Activity { get; }
        public Rect Chat { get; }
    }

    public static class DebugCenterLayout
    {
        public const float WindowInset = 8f;
        public const float ColumnGap = 10f;
        public const float ChatHeight = 34f;
        public const float DetailHeaderHeight = 58f;
        public const float DetailActionHeight = 30f;

        private static readonly TabbedPageTabModel[] LegacyTabs =
        {
            new("legacy", "Legacy", "RimMind.UI.Hub.Tab.Legacy", true, true, null)
        };

        public static HubLayoutRects CalculateHub(Rect inRect)
        {
            Rect body = inRect.InsetSafe(WindowInset);
            Rect header = new Rect(body.x, body.y, body.width, RimMindUiMetrics.HeaderHeight);
            Rect tabHost = new Rect(
                body.x,
                header.yMax + RimMindUiMetrics.Padding,
                body.width,
                Mathf.Max(1f, body.yMax - header.yMax - RimMindUiMetrics.Padding));
            TabbedPageLayoutResult tabs = TabbedPageLayout.Calculate(tabHost, LegacyTabs);

            return new HubLayoutRects(body, header, tabs.TabBar, tabs.Content);
        }

        public static AgentPageLayoutRects CalculateAgentPage(Rect rect)
        {
            SplitPageLayoutResult split = SplitPageLayout.Calculate(rect, 0.28f, 220f, 280f, 360f);
            Rect list = split.List;
            Rect detail = split.Detail;
            Rect header = new Rect(detail.x, detail.y, detail.width, DetailHeaderHeight);
            Rect actions = new Rect(detail.x, header.yMax + RimMindUiMetrics.Padding, detail.width, DetailActionHeight);
            Rect chat = new Rect(detail.x, detail.yMax - ChatHeight, detail.width, ChatHeight);
            Rect activity = new Rect(
                detail.x,
                actions.yMax + RimMindUiMetrics.SectionGap,
                detail.width,
                Mathf.Max(1f, chat.y - actions.yMax - RimMindUiMetrics.SectionGap * 2f));

            return new AgentPageLayoutRects(list, detail, header, actions, activity, chat);
        }
    }
}
