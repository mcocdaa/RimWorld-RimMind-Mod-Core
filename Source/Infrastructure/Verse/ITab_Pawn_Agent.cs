using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Domain.Enums;
using RimMind.Presentation.Agent;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class ITab_Pawn_Agent : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        private Vector2 _scrollPosition = Vector2.zero;

        public ITab_Pawn_Agent()
        {
            size = WinSize;
            labelKey = "RimMind.Agent.ITab.Label";
        }

        private Pawn SelectedPawn => SelThing as Pawn;

        protected override void FillTab()
        {
            var pawn = SelectedPawn;
            if (pawn == null) return;

            var comp = CompPawnAgent.GetComp(pawn);
            var agent = comp?.Agent;
            if (agent == null)
            {
                Widgets.Label(new Rect(10f, 10f, WinSize.x - 20f, 30f),
                    "RimMind.Agent.ITab.NoAgent".Translate());
                return;
            }

            var rect = new Rect(10f, 10f, WinSize.x - 20f, WinSize.y - 20f);
            var contentRect = new Rect(0f, 0f, rect.width - 16f, CalculateContentHeight(agent));

            Widgets.BeginScrollView(rect, ref _scrollPosition, contentRect);

            float curY = 0f;

            // Section 1: Status Bar
            curY = DrawSectionHeader(contentRect, curY, "RimMind.Agent.ITab.Status".Translate());
            curY = DrawStatusLabel(contentRect, curY, "State", agent.State.ToString());
            curY = DrawStatusLabel(contentRect, curY, "Mode", agent.CurrentModeId.Value);

            if (agent is IPawnAgent pawnAgent)
            {
                curY = DrawStatusLabel(contentRect, curY, "WorkflowPhase",
                    pawnAgent.WorkflowPhase.ToString());
                curY = DrawStatusLabel(contentRect, curY, "Autonomy",
                    pawnAgent.AutonomyLevel.ToString());
            }

            curY += 8f;

            // Section 2: Goals
            curY = DrawSectionHeader(contentRect, curY, "RimMind.Agent.ITab.Goals".Translate());
            if (agent is IPawnAgent pa2)
            {
                var goals = pa2.GoalStack.Goals;
                if (goals.Count == 0)
                {
                    curY = DrawStatusLabel(contentRect, curY, "", "  (none)");
                }
                else
                {
                    foreach (var goal in goals)
                    {
                        var goalText = $"  [{goal.Status}] {goal.Description} (P:{goal.Priority:F1})";
                        curY = DrawStatusLabel(contentRect, curY, "", goalText);
                    }
                }
            }

            curY += 8f;

            // Section 3: Strategy Weights
            curY = DrawSectionHeader(contentRect, curY, "RimMind.Agent.ITab.Strategy".Translate());
            if (agent is IPawnAgent pa3)
            {
                var topW = pa3.StrategyOptimizer.GetTopN(5);
                if (topW.Count == 0)
                {
                    curY = DrawStatusLabel(contentRect, curY, "", "  (no data)");
                }
                else
                {
                    foreach (var kv in topW)
                    {
                        curY = DrawStatusLabel(contentRect, curY, "", $"  {kv.Key}: {kv.Value:F2}");
                    }
                }
            }

            curY += 8f;

            // Section 4: Behavior History (from IPawnAgent.BehaviorHistory via GetRecentHistory)
            curY = DrawSectionHeader(contentRect, curY, "RimMind.Agent.ITab.History".Translate());
            if (agent is IPawnAgent pa4)
            {
                var recent = pa4.GetRecentHistory(5);
                if (recent.Count == 0)
                {
                    curY = DrawStatusLabel(contentRect, curY, "", "  (no history)");
                }
                else
                {
                    foreach (var record in recent)
                    {
                        var marker = record.Success ? "OK" : "FAIL";
                        curY = DrawStatusLabel(contentRect, curY, "",
                            $"  [{marker}] {record.Action} - {record.Reason}");
                    }
                }
            }

            Widgets.EndScrollView();
        }

        private float CalculateContentHeight(IAgentControl agent)
        {
            float height = 200f;
            if (agent is IPawnAgent pa)
            {
                height += pa.GoalStack.Goals.Count * 20f;
                height += pa.StrategyOptimizer.GetTopN(5).Count * 20f;
                height += pa.GetRecentHistory(5).Count * 20f;
            }
            return height;
        }

        private float DrawSectionHeader(Rect contentRect, float y, string label)
        {
            Widgets.Label(new Rect(contentRect.x, contentRect.y + y, contentRect.width, 22f),
                $"<b>{label}</b>");
            return y + 24f;
        }

        private float DrawStatusLabel(Rect contentRect, float y, string key, string value)
        {
            var text = string.IsNullOrEmpty(key) ? value : $"{key}: {value}";
            Widgets.Label(new Rect(contentRect.x + 4f, contentRect.y + y, contentRect.width - 8f, 18f),
                text);
            return y + 20f;
        }
    }
}
