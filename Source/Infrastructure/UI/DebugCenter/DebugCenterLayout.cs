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

        public static HubLayoutRects CalculateHub(Rect inRect)
        {
            Rect body = Inset(inRect, WindowInset);
            Rect header = new Rect(body.x, body.y, body.width, RimMindUITheme.HeaderHeight);
            Rect tabs = new Rect(body.x, header.yMax + RimMindUITheme.Padding, body.width, RimMindUITheme.TabHeight);
            Rect content = new Rect(
                body.x,
                tabs.yMax + RimMindUITheme.SectionGap,
                body.width,
                Mathf.Max(1f, body.yMax - tabs.yMax - RimMindUITheme.SectionGap));

            return new HubLayoutRects(body, header, tabs, content);
        }

        public static AgentPageLayoutRects CalculateAgentPage(Rect rect)
        {
            float listWidth = Mathf.Clamp(rect.width * 0.28f, 220f, 280f);
            if (rect.width - listWidth - ColumnGap < 360f)
                listWidth = Mathf.Max(180f, rect.width - 360f - ColumnGap);

            Rect list = new Rect(rect.x, rect.y, listWidth, rect.height);
            Rect detail = new Rect(list.xMax + ColumnGap, rect.y, rect.width - listWidth - ColumnGap, rect.height);

            Rect header = new Rect(detail.x, detail.y, detail.width, DetailHeaderHeight);
            Rect actions = new Rect(detail.x, header.yMax + RimMindUITheme.Padding, detail.width, DetailActionHeight);
            Rect chat = new Rect(detail.x, detail.yMax - ChatHeight, detail.width, ChatHeight);
            Rect activity = new Rect(
                detail.x,
                actions.yMax + RimMindUITheme.SectionGap,
                detail.width,
                Mathf.Max(1f, chat.y - actions.yMax - RimMindUITheme.SectionGap * 2f));

            return new AgentPageLayoutRects(list, detail, header, actions, activity, chat);
        }

        private static Rect Inset(Rect rect, float inset)
        {
            return new Rect(
                rect.x + inset,
                rect.y + inset,
                Mathf.Max(1f, rect.width - inset * 2f),
                Mathf.Max(1f, rect.height - inset * 2f));
        }
    }
}
