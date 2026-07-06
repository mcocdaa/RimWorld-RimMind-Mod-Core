using RimMind.Domain.Enums;
using RimMind.Infrastructure.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public sealed class AgentActivityStreamDrawer
    {
        private Vector2 _activityScrollPos;

        public void Draw(Rect rect, AgentState state, int pendingRequests, RimMindLayoutScope scope)
        {
            Draw(rect, StateLabel(state), pendingRequests, scope);
        }

        public void Draw(Rect rect, string stateLabel, int pendingRequests, RimMindLayoutScope scope)
        {
            scope.Record(rect, "Agents:Activity");
            Widgets.DrawBoxSolid(rect, RimMindUI.ColorSectionBg);
            Rect inner = rect.ContractedBy(RimMindUI.Padding);

            float contentHeight = RimMindUI.LineHeight * 5f + RimMindUI.SectionGap;
            var (bodyRect, _) = RimMindUI.BeginScrollView(inner, ref _activityScrollPos,
                Mathf.Max(inner.height + 1f, contentHeight));

            float y = RimMindUI.DrawSectionHeader(bodyRect, 0f,
                "RimMind.UI.AgentsPage.Activity".Translate());
            y = RimMindUI.DrawKeyValueRow(bodyRect, y,
                "RimMind.UI.AgentsPage.State".Translate(), stateLabel);
            y = RimMindUI.DrawKeyValueRow(bodyRect, y,
                "RimMind.UI.Hub.PendingRequests".Translate(), pendingRequests.ToString());
            RimMindUI.DrawWrappedLabel(bodyRect, y,
                "RimMind.UI.AgentsPage.Activity.Empty".Translate(), RimMindUI.ColorMuted);

            Widgets.EndScrollView();
        }

        public static string StateLabel(AgentState state)
        {
            return state switch
            {
                AgentState.Active => "RimMind.UI.AgentsPage.Active".Translate(),
                AgentState.Paused => "RimMind.UI.AgentsPage.Paused".Translate(),
                AgentState.Terminated => "RimMind.UI.AgentsPage.Terminated".Translate(),
                _ => "RimMind.UI.AgentsPage.Dormant".Translate()
            };
        }
    }
}
