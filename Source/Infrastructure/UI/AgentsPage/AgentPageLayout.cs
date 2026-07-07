using RimMind.Infrastructure.UI.DebugCenter;
using RimMind.Presentation.UI.Framework;
using UnityEngine;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public readonly struct AgentPageRects
    {
        public AgentPageRects(
            Rect list,
            Rect detail,
            Rect status,
            Rect actions,
            Rect activity,
            Rect chat,
            ActionBarLayoutResult actionBar)
        {
            List = list;
            Detail = detail;
            Status = status;
            Actions = actions;
            Activity = activity;
            Chat = chat;
            ActionBar = actionBar;
        }

        public Rect List { get; }
        public Rect Detail { get; }
        public Rect Status { get; }
        public Rect Actions { get; }
        public Rect Activity { get; }
        public Rect Chat { get; }
        public ActionBarLayoutResult ActionBar { get; }
    }

    public static class AgentPageLayout
    {
        public static AgentPageRects Calculate(Rect rect)
        {
            SplitPageLayoutResult split = SplitPageLayout.Calculate(rect, 0.28f, 180f, 280f, 360f);
            Rect detail = split.Detail.InsetSafe(RimMindUiMetrics.Padding);
            Rect status = new Rect(detail.x, detail.y, detail.width, 72f);
            Rect actions = new Rect(detail.x, status.yMax + RimMindUiMetrics.Padding, detail.width, 70f);
            ActionBarLayoutResult actionBar = ActionBarLayout.Calculate(
                actions,
                new[] { "primary", "force_think", "open_requests" });
            Rect chat = new Rect(detail.x, detail.yMax - DebugCenterLayout.ChatHeight, detail.width, DebugCenterLayout.ChatHeight);
            Rect activity = new Rect(
                detail.x,
                actions.yMax + RimMindUiMetrics.SectionGap,
                detail.width,
                Mathf.Max(120f, chat.y - actions.yMax - RimMindUiMetrics.SectionGap * 2f));

            return new AgentPageRects(split.List, detail, status, actions, activity, chat, actionBar);
        }
    }
}
