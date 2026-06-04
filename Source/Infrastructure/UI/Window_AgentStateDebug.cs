using System.Text;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Runtime;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_AgentStateDebug : Window
    {
        private Vector2 _scrollPos = Vector2.zero;
        private const float Padding = 6f;
        private const float LineH = 22f;
        private const float BtnHeight = 24f;

        private string _contextSnapshotSummary = "";
        private Pawn? _targetPawn;
        private IAgentControl? _targetAgent;

        public override Vector2 InitialSize => new Vector2(640f, 520f);

        public Window_AgentStateDebug() : this(pawn: null, agent: null) { }

        public Window_AgentStateDebug(Pawn? pawn) : this(pawn, agent: null) { }

        public Window_AgentStateDebug(IAgentControl agent) : this(pawn: null, agent)
        {
        }

        private Window_AgentStateDebug(Pawn? pawn, IAgentControl? agent)
        {
            _targetPawn = pawn;
            _targetAgent = agent;
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            float headerH = 30f;
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerH);
            GUI.color = new Color(0.7f, 0.8f, 1f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "RimMind.UI.AgentStateDebug.Title".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect bodyRect = new Rect(inRect.x, inRect.y + headerH + Padding,
                inRect.width, inRect.height - headerH - Padding);

            if (_targetAgent is IScopedAgent scopedAgent)
            {
                DrawScopedAgentDetail(bodyRect, scopedAgent);
                return;
            }

            Pawn? pawn = _targetPawn ?? Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                DrawNoPawnState(bodyRect);
                return;
            }

            DrawPawnDetail(bodyRect, pawn);
        }

        private void DrawScopedAgentDetail(Rect rect, IScopedAgent scopedAgent)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.14f, 0.5f));

            float contentH = LineH * 16 + BtnHeight * 3 + Padding * 16;
            Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, contentH);
            Widgets.BeginScrollView(rect, ref _scrollPos, viewRect);

            float x = viewRect.x + Padding;
            float y = viewRect.y + Padding;
            float labelW = viewRect.width - Padding * 2;

            GUI.color = new Color(0.7f, 0.85f, 1f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.ScopedAgentTitle".Translate(scopedAgent.ScopeType));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            y += LineH + Padding;

            GUI.color = new Color(0.85f, 0.9f, 1f);
            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.ScopeId".Translate(scopedAgent.ScopeId));
            GUI.color = Color.white;
            y += LineH + Padding;

            if (scopedAgent.MapId.HasValue)
            {
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.MapId".Translate(scopedAgent.MapId.Value.ToString()));
                y += LineH + Padding;
            }

            string stateKey = $"RimMind.Agent.State.{scopedAgent.State}";
            string stateLabel = stateKey.Translate();
            GUI.color = scopedAgent.State == AgentState.Active
                ? new Color(0.4f, 1f, 0.4f)
                : scopedAgent.State == AgentState.Paused
                    ? new Color(1f, 0.8f, 0.3f)
                    : new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.State".Translate(stateLabel));
            GUI.color = Color.white;
            y += LineH + Padding;

            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.Mode".Translate((string)scopedAgent.CurrentModeId));
            y += LineH + Padding;

            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.NpcId".Translate(scopedAgent.NpcId ?? ""));
            y += LineH + Padding;

            int? lastThinkTick = scopedAgent.LastThinkTick;
            if (lastThinkTick.HasValue && lastThinkTick.Value > 0)
            {
                int elapsed = Find.TickManager.TicksGame - lastThinkTick.Value;
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.LastThinkTick".Translate(elapsed));
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.LastThinkTick".Translate(
                        "RimMind.UI.AgentStateDebug.NoData".Translate()));
                GUI.color = Color.white;
            }
            y += LineH + Padding;

            float successRate = scopedAgent.GetRecentSuccessRate();
            GUI.color = successRate > 0.5f ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.8f, 0.3f);
            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.SuccessRate".Translate(successRate.ToString("P0")));
            GUI.color = Color.white;
            y += LineH + Padding;

            var recentHistory = scopedAgent.GetRecentHistory(5);
            if (recentHistory.Count > 0)
            {
                GUI.color = new Color(0.7f, 0.8f, 1f);
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.RecentBehavior".Translate());
                GUI.color = Color.white;
                y += LineH;

                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                foreach (var record in recentHistory)
                {
                    string recordStr = $"[{(record.Success ? "OK" : "FAIL")}] {record.Action}";
                    float recordH = Text.CalcHeight(recordStr, labelW - Padding);
                    Widgets.Label(new Rect(x + Padding, y, labelW - Padding, recordH), recordStr);
                    y += recordH + 2f;
                }
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.NoBehaviorHistory".Translate());
                GUI.color = Color.white;
            }
            y += LineH + Padding;

            string debugInfo = scopedAgent.GetDebugInfo();
            if (!string.IsNullOrEmpty(debugInfo))
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                float debugH = Text.CalcHeight(debugInfo, labelW);
                Widgets.Label(new Rect(x, y, labelW, debugH), debugInfo);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                y += debugH + Padding;
            }

            y = DrawScopedAgentButtons(x, y, labelW, scopedAgent);

            Widgets.EndScrollView();
        }

        private float DrawScopedAgentButtons(float x, float y, float labelW, IScopedAgent scopedAgent)
        {
            float btnW = 160f;

            Rect forceThinkBtn = new Rect(x, y, btnW, BtnHeight);
            if (Widgets.ButtonText(forceThinkBtn, "RimMind.UI.AgentStateDebug.SendTestRequest".Translate()))
            {
                scopedAgent.ForceThink();
            }

            Rect requestLogBtn = new Rect(x + btnW + Padding, y, btnW, BtnHeight);
            if (Widgets.ButtonText(requestLogBtn, "RimMind.UI.AgentStateDebug.OpenRequestLog".Translate()))
            {
                Find.WindowStack.Add(new Window_RequestLog());
            }
            y += BtnHeight + Padding;

            Rect toolCallBtn = new Rect(x, y, btnW, BtnHeight);
            if (Widgets.ButtonText(toolCallBtn, "RimMind.UI.AgentStateDebug.OpenToolCallDebug".Translate()))
            {
                Find.WindowStack.Add(new Window_ToolCallDebug());
            }

            Rect mechanismBtn = new Rect(x + btnW + Padding, y, btnW, BtnHeight);
            if (Widgets.ButtonText(mechanismBtn, "RimMind.UI.AgentStateDebug.OpenMechanismStatus".Translate()))
            {
                Find.WindowStack.Add(new Window_MechanismStatus());
            }
            y += BtnHeight + Padding;

            Rect destroyBtn = new Rect(x, y, btnW, BtnHeight);
            if (Widgets.ButtonText(destroyBtn, "RimMind.UI.AgentStateDebug.DestroyScopedAgent".Translate()))
            {
                var manager = RimMindServiceLocator.Get<IScopedAgentManager>();
                if (manager != null)
                {
                    manager.Remove(scopedAgent.ScopeType, scopedAgent.ScopeId);
                    Close();
                }
            }
            y += BtnHeight + Padding;

            return y;
        }

        private void DrawNoPawnState(Rect rect)
        {
            float centerX = rect.x + rect.width / 2f;
            float centerY = rect.y + rect.height / 2f;

            GUI.color = Color.grey;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, centerY - 20f, rect.width, LineH),
                "RimMind.UI.AgentStateDebug.NoPawn".Translate());

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(new Rect(rect.x, centerY + 4f, rect.width, LineH),
                "RimMind.UI.AgentStateDebug.NoPawnHint".Translate());

            var queue = RimMindServiceLocator.Get<IAIRequestQueue>();
            if (queue != null)
            {
                string queueInfo = queue.IsPaused
                    ? "RimMind.UI.AgentStateDebug.QueuePaused".Translate()
                    : "RimMind.UI.AgentStateDebug.QueueRunning".Translate(
                        queue.ActiveRequestCount.ToString(), queue.TotalQueuedCount.ToString());
                float queueInfoH = Text.CalcHeight(queueInfo, rect.width - 24f);
                Widgets.Label(new Rect(rect.x + 12f, centerY + 24f, rect.width - 24f, queueInfoH), queueInfo);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawPawnDetail(Rect rect, Pawn pawn)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.14f, 0.5f));

            float contentH = LineH * 20 + BtnHeight * 6 + Padding * 20;
            Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, contentH);
            Widgets.BeginScrollView(rect, ref _scrollPos, viewRect);

            float x = viewRect.x + Padding;
            float y = viewRect.y + Padding;
            float labelW = viewRect.width - Padding * 2;

            string pawnLabel = pawn.Name?.ToStringShort ?? pawn.LabelShort;
            string pawnInfo = "RimMind.UI.AgentStateDebug.PawnInfo".Translate(pawnLabel, pawn.thingIDNumber);
            GUI.color = new Color(0.85f, 0.9f, 1f);
            Widgets.Label(new Rect(x, y, labelW, LineH), pawnInfo);
            GUI.color = Color.white;
            y += LineH + Padding;

            string npcId = $"NPC-{pawn.thingIDNumber}";
            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.NpcId".Translate(npcId));
            y += LineH + Padding;

            CompPawnAgent? comp = CompPawnAgent.GetComp(pawn);
            IAgentControl? agent = comp?.Agent;

            if (agent != null)
            {
                GUI.color = new Color(0.4f, 1f, 0.4f);
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.AgentExists".Translate());
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.5f, 0.4f);
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.AgentMissing".Translate());
                GUI.color = Color.white;
                y += LineH + Padding;

                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.AgentMissingHint".Translate());
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
            y += LineH + Padding;

            if (agent != null)
            {
                string stateKey = $"RimMind.Agent.State.{agent.State}";
                string stateLabel = stateKey.Translate();
                GUI.color = agent.State == AgentState.Active
                    ? new Color(0.4f, 1f, 0.4f)
                    : agent.State == AgentState.Paused
                        ? new Color(1f, 0.8f, 0.3f)
                        : new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.State".Translate(stateLabel));
                GUI.color = Color.white;
                y += LineH + Padding;

                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.Mode".Translate((string)agent.CurrentModeId));
                y += LineH + Padding;

                bool isThinking = agent is IPawnAgent pawnAgent
                    && pawnAgent.WorkflowPhase == AgentWorkflowPhase.Thinking;
                GUI.color = isThinking ? new Color(0.4f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(x, y, labelW, LineH),
                    "RimMind.UI.AgentStateDebug.Thinking".Translate(isThinking.ToString()));
                GUI.color = Color.white;
                y += LineH + Padding;

                int? lastThinkTick = agent.LastThinkTick;
                if (lastThinkTick.HasValue && lastThinkTick.Value > 0)
                {
                    int elapsed = Find.TickManager.TicksGame - lastThinkTick.Value;
                    Widgets.Label(new Rect(x, y, labelW, LineH),
                        "RimMind.UI.AgentStateDebug.LastThinkTick".Translate(elapsed));
                }
                else
                {
                    GUI.color = Color.grey;
                    Widgets.Label(new Rect(x, y, labelW, LineH),
                        "RimMind.UI.AgentStateDebug.LastThinkTick".Translate(
                            "RimMind.UI.AgentStateDebug.NoData".Translate()));
                    GUI.color = Color.white;
                }
                y += LineH + Padding;
            }
            else
            {
                y += LineH * 4 + Padding * 4;
            }

            GUI.color = new Color(0.7f, 0.8f, 1f);
            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.QueueState".Translate());
            GUI.color = Color.white;
            y += LineH;

            var queue = RimMindServiceLocator.Get<IAIRequestQueue>();
            if (queue != null)
            {
                string qSummary = $"Paused={queue.IsPaused} Active={queue.ActiveRequestCount} Queued={queue.TotalQueuedCount}";
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(x + Padding, y, labelW - Padding, LineH), qSummary);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(x + Padding, y, labelW - Padding, LineH),
                    "RimMind.UI.AgentStateDebug.NoData".Translate());
                GUI.color = Color.white;
            }
            y += LineH + Padding;

            GUI.color = new Color(0.7f, 0.8f, 1f);
            Widgets.Label(new Rect(x, y, labelW, LineH),
                "RimMind.UI.AgentStateDebug.ContextSnapshot".Translate());
            GUI.color = Color.white;
            y += LineH;

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            if (!_contextSnapshotSummary.NullOrEmpty())
            {
                float snapH = Text.CalcHeight(_contextSnapshotSummary, labelW - Padding);
                Widgets.Label(new Rect(x + Padding, y, labelW - Padding, snapH), _contextSnapshotSummary);
            }
            else
            {
                Widgets.Label(new Rect(x + Padding, y, labelW - Padding, LineH),
                    "RimMind.UI.AgentStateDebug.NoData".Translate());
            }
            GUI.color = Color.white;
            y += LineH + Padding;

            y = DrawButtons(x, y, labelW, pawn);

            Widgets.EndScrollView();
        }

        private float DrawButtons(float x, float y, float labelW, Pawn pawn)
        {
            float btnW = 160f;

            Rect createAgentBtn = new Rect(x, y, btnW, BtnHeight);
            if (Widgets.ButtonText(createAgentBtn, "RimMind.UI.AgentStateDebug.CreateAgent".Translate()))
            {
                var factory = RimMindServiceLocator.Get<IPawnAgentFactory>();
                var agentBus = RimMindServiceLocator.Get<IAgentBus>();
                if (factory != null && agentBus != null)
                {
                    var createdAgent = factory.Create(pawn, agentBus);
                    if (createdAgent != null)
                    {
                        var comp = CompPawnAgent.GetComp(pawn);
                        if (comp != null && comp.Agent == null)
                            comp.Agent = createdAgent;
                    }
                }
            }

            Rect buildContextBtn = new Rect(x + btnW + Padding, y, btnW, BtnHeight);
            if (Widgets.ButtonText(buildContextBtn, "RimMind.UI.AgentStateDebug.BuildContext".Translate()))
            {
                string npcId = $"NPC-{pawn.thingIDNumber}";
                var contextEngine = RimMindServiceLocator.Get<IContextBuilder>();
                if (contextEngine != null)
                {
                    var snapshot = contextEngine.BuildSnapshotFromEnvelope(npcId, "[Debug] AgentStateDebug");
                    if (snapshot != null)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Tokens: {snapshot.EstimatedTokens}");
                        sb.AppendLine($"L0={snapshot.Meta.L0Tokens} L1={snapshot.Meta.L1Tokens} L2={snapshot.Meta.L2Tokens} L3={snapshot.Meta.L3Tokens} L4={snapshot.Meta.L4Tokens}");
                        sb.AppendLine($"Messages: {snapshot.Messages.Count}");
                        _contextSnapshotSummary = sb.ToString();
                    }
                    else
                    {
                        _contextSnapshotSummary = "RimMind.UI.AgentStateDebug.NoData".Translate();
                    }
                }
                else
                {
                    _contextSnapshotSummary = "RimMind.UI.AgentStateDebug.NoData".Translate();
                }
            }
            y += BtnHeight + Padding;

            Rect testThinkBtn = new Rect(x, y, btnW, BtnHeight);
            if (Widgets.ButtonText(testThinkBtn, "RimMind.UI.AgentStateDebug.SendTestRequest".Translate()))
            {
                var comp = CompPawnAgent.GetComp(pawn);
                if (comp?.Agent != null)
                    comp.Agent.ForceThink();
            }

            Rect requestLogBtn = new Rect(x + btnW + Padding, y, btnW, BtnHeight);
            if (Widgets.ButtonText(requestLogBtn, "RimMind.UI.AgentStateDebug.OpenRequestLog".Translate()))
            {
                Find.WindowStack.Add(new Window_RequestLog());
            }
            y += BtnHeight + Padding;

            Rect toolCallBtn = new Rect(x, y, btnW, BtnHeight);
            if (Widgets.ButtonText(toolCallBtn, "RimMind.UI.AgentStateDebug.OpenToolCallDebug".Translate()))
            {
                Find.WindowStack.Add(new Window_ToolCallDebug());
            }

            Rect mechanismBtn = new Rect(x + btnW + Padding, y, btnW, BtnHeight);
            if (Widgets.ButtonText(mechanismBtn, "RimMind.UI.AgentStateDebug.OpenMechanismStatus".Translate()))
            {
                Find.WindowStack.Add(new Window_MechanismStatus());
            }
            y += BtnHeight + Padding;

            return y;
        }
    }
}
