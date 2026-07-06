using RimMind.Infrastructure.UI.DebugCenter;
using UnityEngine;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public readonly struct AgentPageRects
    {
        public AgentPageRects(Rect list, Rect detail, Rect status, Rect actions, Rect activity, Rect chat)
        {
            List = list;
            Detail = detail;
            Status = status;
            Actions = actions;
            Activity = activity;
            Chat = chat;
        }

        public Rect List { get; }
        public Rect Detail { get; }
        public Rect Status { get; }
        public Rect Actions { get; }
        public Rect Activity { get; }
        public Rect Chat { get; }
    }

    public static class AgentPageLayout
    {
        public static AgentPageRects Calculate(Rect rect)
        {
            AgentPageLayoutRects baseLayout = DebugCenterLayout.CalculateAgentPage(rect);
            Rect detail = Inset(baseLayout.Detail, RimMindUITheme.Padding);
            Rect status = new Rect(detail.x, detail.y, detail.width, 72f);
            Rect actions = new Rect(detail.x, status.yMax + RimMindUITheme.Padding, detail.width, 34f);
            Rect chat = new Rect(detail.x, detail.yMax - DebugCenterLayout.ChatHeight, detail.width, DebugCenterLayout.ChatHeight);
            Rect activity = new Rect(
                detail.x,
                actions.yMax + RimMindUITheme.SectionGap,
                detail.width,
                Mathf.Max(120f, chat.y - actions.yMax - RimMindUITheme.SectionGap * 2f));

            return new AgentPageRects(baseLayout.List, detail, status, actions, activity, chat);
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
