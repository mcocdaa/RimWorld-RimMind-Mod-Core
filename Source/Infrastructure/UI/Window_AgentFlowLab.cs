using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Application.Features.Agent;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Common;
using RimMind.Domain.Enums;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation.Agent;
using RimMind.Application.Api;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_AgentFlowLab : Window
    {
        private enum FlowLabStep
        {
            SelectTarget,
            CreateAgent,
            BuildContext,
            SendRequest,
            ParseDecision,
            MapMechanism,
            DryRun,
            Execute
        }

        private enum AgentFlowScope
        {
            Pawn,
            Map,
            Colony,
            Storyteller,
            Global
        }

        private enum StepStatus
        {
            Pending,
            Active,
            Completed,
            Failed
        }

        private Vector2 _scrollPos = Vector2.zero;
        private const float Padding = 6f;
        private const float LineH = 22f;
        private const float BtnHeight = 24f;
        private const float SectionGap = 10f;

        private Pawn? _selectedPawn;
        private IAgentControl? _agent;
        private IScopedAgent? _scopedAgent;
        private ContextSnapshot? _lastSnapshot;
        private string _requestStatus = "";
        private string _lastError = "";
        private string _lastDecisionInfo = "";
        private string _mappedMechanismsInfo = "";
        private string _queueInfo = "";
        private Pawn? _initialPawn;

        private bool _offlineMode = true;
        private bool _dryRunCompleted;
        private AgentDecision? _lastDecision;
        private MechanismWriteArgs? _lastWriteArgs;
        private MechanismOperationType _lastOperationType;
        private AgentFlowScope _selectedScope = AgentFlowScope.Pawn;
        private string _dryRunResult = "";
        private string _parsedDecisionInfo = "";
        private string _validationInfo = "";

        private readonly Dictionary<FlowLabStep, StepStatus> _stepStatuses = new();

        public override Vector2 InitialSize => new Vector2(780f, 620f);

        public Window_AgentFlowLab() : this(null) { }

        public Window_AgentFlowLab(Pawn? pawn)
        {
            _initialPawn = pawn;
            _selectedPawn = pawn;
            _lastOperationType = MechanismOperationType.Set;
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
            ResetStepStatuses();
        }

        private void ResetStepStatuses()
        {
            foreach (FlowLabStep step in Enum.GetValues(typeof(FlowLabStep)))
                _stepStatuses[step] = StepStatus.Pending;
        }

        private void SetStepStatus(FlowLabStep step, StepStatus status)
        {
            _stepStatuses[step] = status;
        }

        private Color StepStatusColor(FlowLabStep step)
        {
            return _stepStatuses.TryGetValue(step, out var status) ? status switch
            {
                StepStatus.Completed => new Color(0.4f, 1f, 0.4f),
                StepStatus.Active => new Color(1f, 1f, 0.4f),
                StepStatus.Failed => new Color(1f, 0.4f, 0.4f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            } : Color.grey;
        }

        private string StepStatusSymbol(FlowLabStep step)
        {
            return _stepStatuses.TryGetValue(step, out var status) ? status switch
            {
                StepStatus.Completed => "\u2713",
                StepStatus.Active => "\u25B6",
                StepStatus.Failed => "\u2717",
                _ => "\u25CB"
            } : "\u25CB";
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            float contentH = CalcTotalContentHeight();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentH);
            Widgets.BeginScrollView(inRect, ref _scrollPos, viewRect);

            float y = 0f;
            float w = viewRect.width;

            DrawSectionHeader(ref y, w, "RimMind.UI.AgentFlowLab.Title");
            DrawOfflineModeToggle(ref y, w);
            DrawScopeSelector(ref y, w);
            DrawPawnSelection(ref y, w);
            DrawAgentLifecycle(ref y, w);
            DrawContextBuilding(ref y, w);
            DrawLlmRequest(ref y, w);
            DrawDecisionParsing(ref y, w);
            DrawMechanismMapping(ref y, w);
            DrawQueueState(ref y, w);
            DrawOpenLogs(ref y, w);
            DrawErrorLog(ref y, w);

            Widgets.EndScrollView();
        }

        private float CalcTotalContentHeight()
        {
            float h = LineH + Padding;
            h += BtnHeight + LineH + Padding + SectionGap;
            h += LineH + BtnHeight + LineH * 2f + Padding * 2f + SectionGap;
            h += LineH + BtnHeight + Padding + SectionGap;
            h += LineH + LineH + BtnHeight + Padding + SectionGap;
            h += LineH + BtnHeight + LineH + LineH + Padding + SectionGap;
            h += LineH + BtnHeight + LineH + LineH + Padding + SectionGap;
            h += LineH + LineH + LineH + Padding + SectionGap;
            h += LineH + BtnHeight + LineH + BtnHeight + LineH + LineH + Padding + SectionGap;
            h += LineH + LineH + Padding + SectionGap;
            h += LineH + BtnHeight * 4 + Padding + SectionGap;
            h += LineH + BtnHeight + Padding;
            return h + Padding * 4;
        }

        private void DrawSectionHeader(ref float y, float w, string key)
        {
            GUI.color = new Color(0.7f, 0.8f, 1f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, w, LineH + 4f), key.Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += LineH + Padding;
        }

        private void DrawStepHeader(ref float y, float w, string key, FlowLabStep step)
        {
            var symbol = StepStatusSymbol(step);
            var color = StepStatusColor(step);
            GUI.color = color;
            Text.Font = GameFont.Small;
            string headerText = $"{symbol} {key.Translate()}";
            Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH), headerText);
            GUI.color = Color.white;
            y += LineH;
        }

        private void DrawSectionLabel(ref float y, float w, string key)
        {
            GUI.color = new Color(0.6f, 0.75f, 1f);
            Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH), key.Translate());
            GUI.color = Color.white;
            y += LineH;
        }

        private void DrawLabel(ref float y, float w, string text, GameFont font)
        {
            Text.Font = font;
            Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH), text);
            Text.Font = GameFont.Small;
            y += LineH;
        }

        private void DrawOfflineModeToggle(ref float y, float w)
        {
            string modeLabel = _offlineMode
                ? "RimMind.UI.AgentFlowLab.OfflineMode".Translate()
                : "RimMind.UI.AgentFlowLab.LiveMode".Translate();

            GUI.color = _offlineMode ? new Color(0.6f, 0.8f, 1f) : new Color(1f, 0.6f, 0.4f);
            Rect toggleBtn = new Rect(Padding, y, 200f, BtnHeight);
            if (Widgets.ButtonText(toggleBtn, modeLabel))
            {
                _offlineMode = !_offlineMode;
            }
            GUI.color = Color.white;

            string modeHint = _offlineMode
                ? "RimMind.UI.AgentFlowLab.OfflineHint".Translate()
                : "RimMind.UI.AgentFlowLab.LiveHint".Translate();
            GUI.color = Color.grey;
            Widgets.Label(new Rect(210f, y, w - 210f - Padding, BtnHeight), modeHint);
            GUI.color = Color.white;
            y += BtnHeight + LineH + Padding;

            y += SectionGap;
        }

        private void DrawScopeSelector(ref float y, float w)
        {
            DrawLabel(ref y, w, "RimMind.UI.AgentFlowLab.Scope".Translate(), GameFont.Small);

            float x = Padding;
            DrawScopeButton(ref x, y, 110f, AgentFlowScope.Pawn, "RimMind.UI.AgentFlowLab.ScopePawn");
            DrawScopeButton(ref x, y, 110f, AgentFlowScope.Map, "RimMind.UI.AgentFlowLab.ScopeMap");
            DrawScopeButton(ref x, y, 110f, AgentFlowScope.Colony, "RimMind.UI.AgentFlowLab.ScopeColony");
            DrawScopeButton(ref x, y, 130f, AgentFlowScope.Storyteller, "RimMind.UI.AgentFlowLab.ScopeStoryteller");
            DrawScopeButton(ref x, y, 110f, AgentFlowScope.Global, "RimMind.UI.AgentFlowLab.ScopeGlobal");
            y += BtnHeight + Padding;

            if (_selectedScope != AgentFlowScope.Pawn)
            {
                GUI.color = new Color(0.7f, 0.9f, 1f);
                string scopeHint = "RimMind.UI.AgentFlowLab.ScopeHint".Translate(_selectedScope.ToString());
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, Text.LineHeight * 2f), scopeHint);
                GUI.color = Color.white;
                y += Text.LineHeight * 2f + Padding;
            }

            y += SectionGap;
        }

        private void DrawScopeButton(ref float x, float y, float width, AgentFlowScope scope, string labelKey)
        {
            bool selected = _selectedScope == scope;
            GUI.color = selected ? new Color(0.45f, 0.85f, 1f) : Color.white;
            if (Widgets.ButtonText(new Rect(x, y, width, BtnHeight), labelKey.Translate()))
            {
                if (_selectedScope != scope)
                {
                    _selectedScope = scope;
                    _scopedAgent = null;
                    _agent = null;
                    ResetStepStatuses();
                }
            }
            GUI.color = Color.white;
            x += width + 6f;
        }

        private bool DrawNonPawnScope(ref float y, float w)
        {
            if (_selectedScope == AgentFlowScope.Pawn)
                return false;

            if (_scopedAgent == null)
            {
                var manager = RimMindServiceLocator.Get<IScopedAgentManager>();
                var agentBus = RimMindServiceLocator.Get<IAgentBus>();
                if (manager != null && agentBus != null)
                {
                    string scopeType = _selectedScope.ToString();
                    string scopeId = ResolveScopeId(_selectedScope);
                    int? mapId = _selectedScope == AgentFlowScope.Map ? Find.CurrentMap?.Index : null;
                    _scopedAgent = manager.GetOrCreate(scopeType, scopeId, agentBus, mapId);
                    _agent = _scopedAgent;
                    SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Completed);
                }
            }

            if (_scopedAgent != null)
            {
                GUI.color = new Color(0.4f, 1f, 0.4f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.ScopedAgentActive".Translate(
                        _scopedAgent.ScopeType, _scopedAgent.ScopeId, _scopedAgent.State));
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.5f, 0.4f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.ScopeUnsupported".Translate());
                GUI.color = Color.white;
            }
            y += LineH + SectionGap;
            return _scopedAgent == null;
        }

        private string ResolveScopeId(AgentFlowScope scope)
        {
            return scope switch
            {
                AgentFlowScope.Map => Find.CurrentMap?.ToString() ?? "no_map",
                AgentFlowScope.Colony => Find.World?.info?.name ?? "colony",
                AgentFlowScope.Storyteller => Find.Storyteller?.def?.defName ?? "storyteller",
                AgentFlowScope.Global => "global",
                _ => "unknown"
            };
        }

        private void DrawPawnSelection(ref float y, float w)
        {
            DrawStepHeader(ref y, w, "RimMind.UI.AgentFlowLab.SelectedPawn", FlowLabStep.SelectTarget);

            if (DrawNonPawnScope(ref y, w))
                return;

            if (_selectedPawn != null && _selectedPawn.Destroyed)
                _selectedPawn = null;

            if (_selectedPawn != null)
            {
                string name = _selectedPawn.Name?.ToStringShort ?? _selectedPawn.LabelShort;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2 - 80f, LineH),
                    "RimMind.UI.AgentFlowLab.SelectedPawn".Translate(name));
                SetStepStatus(FlowLabStep.SelectTarget, StepStatus.Completed);
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2 - 80f, LineH),
                    "RimMind.UI.AgentFlowLab.NoPawn".Translate());
                GUI.color = Color.white;
                SetStepStatus(FlowLabStep.SelectTarget, StepStatus.Pending);
            }

            Rect refreshBtn = new Rect(w - 80f, y, 74f, BtnHeight);
            if (Widgets.ButtonText(refreshBtn, "RimMind.UI.AgentFlowLab.Refresh".Translate()))
            {
                _selectedPawn = _initialPawn ?? Find.Selector.SingleSelectedThing as Pawn;
                _initialPawn = null;
                _agent = null;
                _lastSnapshot = null;
                _lastDecisionInfo = "";
                _mappedMechanismsInfo = "";
                _dryRunCompleted = false;
                _dryRunResult = "";
                _lastDecision = null;
                _lastWriteArgs = null;
                _lastOperationType = MechanismOperationType.Set;
                _parsedDecisionInfo = "";
                _validationInfo = "";
                ResetStepStatuses();
                if (_selectedPawn != null)
                    SetStepStatus(FlowLabStep.SelectTarget, StepStatus.Completed);
            }
            y += LineH + Padding;

            y += SectionGap;
        }

        private void DrawAgentLifecycle(ref float y, float w)
        {
            DrawStepHeader(ref y, w, "RimMind.UI.AgentFlowLab.AgentLifecycle", FlowLabStep.CreateAgent);

            if (DrawNonPawnScope(ref y, w))
                return;

            if (_selectedPawn != null)
            {
                var comp = CompPawnAgent.GetComp(_selectedPawn);
                _agent = comp?.Agent;

                if (_agent != null)
                {
                    string stateStr = _agent.State.ToString();
                    GUI.color = new Color(0.4f, 1f, 0.4f);
                    Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                        "RimMind.UI.AgentFlowLab.AgentExists".Translate(stateStr));
                    GUI.color = Color.white;
                    SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Completed);
                }
                else
                {
                    GUI.color = new Color(1f, 0.5f, 0.4f);
                    Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                        "RimMind.UI.AgentFlowLab.AgentMissing".Translate());
                    GUI.color = Color.white;
                    SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Pending);
                }
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.NoPawn".Translate());
                GUI.color = Color.white;
            }
            y += LineH;

            Rect createBtn = new Rect(Padding, y, 160f, BtnHeight);
            if (Widgets.ButtonText(createBtn, "RimMind.UI.AgentFlowLab.CreateAgent".Translate()))
            {
                if (_selectedPawn != null)
                {
                    try
                    {
                        SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Active);
                        var comp = CompPawnAgent.GetComp(_selectedPawn);
                        if (comp != null && comp.Agent != null)
                        {
                            _agent = comp.Agent;
                            SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Completed);
                        }
                        else
                        {
                            var factory = RimMindServiceLocator.Get<IPawnAgentFactoryVerse>();
                            var agentBus = RimMindServiceLocator.Get<IAgentBus>();
                            if (factory != null && agentBus != null)
                            {
                                var pawnAgent = factory.Create(_selectedPawn, agentBus);
                                if (pawnAgent != null)
                                {
                                    pawnAgent.TransitionTo(AgentState.Active);
                                    _agent = pawnAgent as IAgentControl;
                                    SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Completed);
                                }
                                else
                                {
                                    _lastError = "IPawnAgentFactory.Create returned null";
                                    SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Failed);
                                }
                            }
                            else
                            {
                                _lastError = "IPawnAgentFactoryVerse or IAgentBus not available";
                                SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Failed);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _lastError = $"CreateAgent: {ex.Message}";
                        SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Failed);
                    }
                }
            }
            y += BtnHeight + Padding;

            y += SectionGap;
        }

        private void DrawContextBuilding(ref float y, float w)
        {
            DrawStepHeader(ref y, w, "RimMind.UI.AgentFlowLab.ContextBuilding", FlowLabStep.BuildContext);

            if (DrawNonPawnScope(ref y, w))
                return;

            Rect buildBtn = new Rect(Padding, y, 180f, BtnHeight);
            if (Widgets.ButtonText(buildBtn, "RimMind.UI.AgentFlowLab.BuildContext".Translate()))
            {
                if (_selectedPawn != null)
                {
                    try
                    {
                        SetStepStatus(FlowLabStep.BuildContext, StepStatus.Active);
                        var contextEngine = RimMindServiceLocator.Get<IContextEngine>() as IContextBuilder;
                        if (contextEngine != null)
                        {
                            string npcId = $"NPC-{_selectedPawn.thingIDNumber}";
                            _lastSnapshot = contextEngine.BuildSnapshotFromEnvelope(npcId, "[AgentFlowLab] Build context");
                            SetStepStatus(FlowLabStep.BuildContext, _lastSnapshot != null ? StepStatus.Completed : StepStatus.Failed);
                        }
                        else
                        {
                            _lastError = "IContextBuilder not available";
                            SetStepStatus(FlowLabStep.BuildContext, StepStatus.Failed);
                        }
                    }
                    catch (Exception ex)
                    {
                        _lastError = $"BuildContext: {ex.Message}";
                        SetStepStatus(FlowLabStep.BuildContext, StepStatus.Failed);
                    }
                }
            }
            y += BtnHeight + Padding;

            if (_lastSnapshot != null)
            {
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.TokenCount".Translate(_lastSnapshot.EstimatedTokens.ToString()));
                y += LineH;

                int msgCount = _lastSnapshot.Messages?.Count ?? 0;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.Messages".Translate(msgCount.ToString()));
                y += LineH;

                if (msgCount > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var msg in _lastSnapshot.Messages.Take(8))
                    {
                        int len = msg.Content?.Length ?? 0;
                        sb.AppendLine($"[{msg.Role}] {len}ch");
                    }
                    if (msgCount > 8)
                        sb.AppendLine($"... +{msgCount - 8} more");
                    float summaryH = Text.CalcHeight(sb.ToString(), w - Padding * 2);
                    GUI.color = new Color(0.7f, 0.7f, 0.7f);
                    Widgets.Label(new Rect(Padding, y, w - Padding * 2, summaryH), sb.ToString());
                    GUI.color = Color.white;
                    y += summaryH + Padding;
                }
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.NoData".Translate());
                GUI.color = Color.white;
                y += LineH;
            }

            y += SectionGap;
        }

        private void DrawLlmRequest(ref float y, float w)
        {
            DrawStepHeader(ref y, w, "RimMind.UI.AgentFlowLab.LlmRequest", FlowLabStep.SendRequest);

            if (DrawNonPawnScope(ref y, w))
                return;

            Rect sendBtn = new Rect(Padding, y, 180f, BtnHeight);
            if (Widgets.ButtonText(sendBtn, "RimMind.UI.AgentFlowLab.SendTestRequest".Translate()))
            {
                if (_selectedPawn != null)
                {
                    try
                    {
                        SetStepStatus(FlowLabStep.SendRequest, StepStatus.Active);
                        _dryRunCompleted = false;
                        _dryRunResult = "";
                        _lastDecision = null;
                        _lastWriteArgs = null;
                        _lastOperationType = MechanismOperationType.Set;
                        _parsedDecisionInfo = "";
                        _validationInfo = "";

                        if (_offlineMode)
                        {
                            HandleOfflineRequest();
                        }
                        else
                        {
                            HandleLiveRequest();
                        }
                    }
                    catch (Exception ex)
                    {
                        _requestStatus = "Failed";
                        _lastError = $"SendRequest: {ex.Message}";
                        SetStepStatus(FlowLabStep.SendRequest, StepStatus.Failed);
                    }
                }
            }
            y += BtnHeight + Padding;

            string statusDisplay = string.IsNullOrEmpty(_requestStatus)
                ? "RimMind.UI.AgentFlowLab.NoData".Translate()
                : _requestStatus;
            Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                "RimMind.UI.AgentFlowLab.RequestStatus".Translate(statusDisplay));
            y += LineH;

            if (_offlineMode)
            {
                GUI.color = new Color(0.6f, 0.8f, 1f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.OfflineStubUsed".Translate());
                GUI.color = Color.white;
                y += LineH;
            }

            y += SectionGap;
        }

        private void HandleOfflineRequest()
        {
            _requestStatus = "Completed (Offline)";

            string stubResponse = "{\"action\":\"pawn.job.force_rest\",\"reason\":\"stub: offline test response\",\"param\":null}";
            _lastDecisionInfo = stubResponse;

            try
            {
                _lastDecision = new AgentDecision(
                    ActionIntent: "pawn.job.force_rest",
                    Reason: "stub: offline test response",
                    Param: null);
                _parsedDecisionInfo = FormatDecision(_lastDecision);
                SetStepStatus(FlowLabStep.SendRequest, StepStatus.Completed);
                SetStepStatus(FlowLabStep.ParseDecision, StepStatus.Completed);
                AutoDryRun();
            }
            catch (Exception ex)
            {
                _lastError = $"Offline parse: {ex.Message}";
                SetStepStatus(FlowLabStep.SendRequest, StepStatus.Completed);
                SetStepStatus(FlowLabStep.ParseDecision, StepStatus.Failed);
            }
        }

        private void HandleLiveRequest()
        {
            var settings = RimMindServiceLocator.Get<ISettingsProvider>();
            if (settings == null || !settings.IsConfigured)
            {
                _requestStatus = "NotConfigured";
                _lastError = "API not configured";
                SetStepStatus(FlowLabStep.SendRequest, StepStatus.Failed);
                return;
            }

            _requestStatus = "Pending";

            string npcId = $"NPC-{_selectedPawn!.thingIDNumber}";
            var envelope = LlmRequestEnvelopeBuilder
                .ForScenario("AgentFlowLab")
                .WithModId("AgentFlowLab")
                .WithNpcId(npcId)
                .WithMaxTokens(200)
                .WithTemperature(0f)
                .WithPriority(AIRequestPriority.High)
                .Build();

            envelope.Messages.Add(new ChatMessage { Role = "system", Content = "You are a test assistant. Reply with one actionable <Action> JSON block." });
            envelope.Messages.Add(new ChatMessage { Role = "user", Content = "Reply exactly like this: <Action>{\"action\":\"pawn.job.force_rest\",\"reason\":\"live test response\",\"param\":null}</Action>" });

            RimMindAPI.Request.Send(envelope, result =>
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (result.IsOk)
                    {
                        _requestStatus = "Completed";
                        _lastDecisionInfo = result.Value.Content ?? "(empty)";
                        SetStepStatus(FlowLabStep.SendRequest, StepStatus.Completed);

                        try
                        {
                            var parseResult = ThinkStrategyHelper.ParseDecisionCore(new LlmResponse
                            {
                                Content = _lastDecisionInfo
                            });

                            if (parseResult.IsOk)
                            {
                                _lastDecision = parseResult.Value;
                                _parsedDecisionInfo = FormatDecision(_lastDecision);
                                SetStepStatus(FlowLabStep.ParseDecision, StepStatus.Completed);
                                AutoDryRun();
                            }
                            else
                            {
                                _parsedDecisionInfo = parseResult.Error.Message;
                                SetStepStatus(FlowLabStep.ParseDecision, StepStatus.Failed);
                            }
                        }
                        catch (Exception ex)
                        {
                            _lastError = $"Parse decision: {ex.Message}";
                            SetStepStatus(FlowLabStep.ParseDecision, StepStatus.Failed);
                        }
                    }
                    else
                    {
                        _requestStatus = "Failed";
                        _lastError = result.Error.Message;
                        SetStepStatus(FlowLabStep.SendRequest, StepStatus.Failed);
                    }
                });
            });
        }

        private void DrawDecisionParsing(ref float y, float w)
        {
            DrawStepHeader(ref y, w, "RimMind.UI.AgentFlowLab.DecisionParsing", FlowLabStep.ParseDecision);

            if (!string.IsNullOrEmpty(_parsedDecisionInfo))
            {
                float h = Text.CalcHeight(_parsedDecisionInfo, w - Padding * 2);
                h = Mathf.Min(h, 60f);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, h), _parsedDecisionInfo);
                GUI.color = Color.white;
                y += h + Padding;
            }
            else if (!string.IsNullOrEmpty(_lastDecisionInfo))
            {
                float h = Text.CalcHeight(_lastDecisionInfo, w - Padding * 2);
                h = Mathf.Min(h, 60f);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, h), _lastDecisionInfo);
                GUI.color = Color.white;
                y += h + Padding;
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.NoData".Translate());
                GUI.color = Color.white;
                y += LineH;
            }

            if (!string.IsNullOrEmpty(_validationInfo))
            {
                GUI.color = new Color(1f, 0.7f, 0.3f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.ValidationResult".Translate(_validationInfo));
                GUI.color = Color.white;
                y += LineH;
            }

            y += SectionGap;
        }

        private void DrawMechanismMapping(ref float y, float w)
        {
            DrawStepHeader(ref y, w, "RimMind.UI.AgentFlowLab.MechanismMapping", FlowLabStep.MapMechanism);

            if (DrawNonPawnScope(ref y, w))
                return;

            Rect dryRunBtn = new Rect(Padding, y, 200f, BtnHeight);
            if (Widgets.ButtonText(dryRunBtn, "RimMind.UI.AgentFlowLab.DryRun".Translate()))
            {
                try
                {
                    PerformDryRun();
                }
                catch (Exception ex)
                {
                    _lastError = $"DryRun: {ex.Message}";
                    SetStepStatus(FlowLabStep.DryRun, StepStatus.Failed);
                }
            }
            y += BtnHeight + Padding;

            if (!string.IsNullOrEmpty(_dryRunResult))
            {
                float h = Text.CalcHeight(_dryRunResult, w - Padding * 2);
                h = Mathf.Min(h, 80f);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, h), _dryRunResult);
                GUI.color = Color.white;
                y += h + Padding;
            }
            else if (!string.IsNullOrEmpty(_mappedMechanismsInfo))
            {
                float h = Text.CalcHeight(_mappedMechanismsInfo, w - Padding * 2);
                h = Mathf.Min(h, 80f);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, h), _mappedMechanismsInfo);
                GUI.color = Color.white;
                y += h + Padding;
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, LineH),
                    "RimMind.UI.AgentFlowLab.NoData".Translate());
                GUI.color = Color.white;
                y += LineH;
            }

            if (!_dryRunCompleted)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                Rect execBtn = new Rect(Padding, y, 220f, BtnHeight);
                Widgets.ButtonText(execBtn, "RimMind.UI.AgentFlowLab.ExecuteRequiresDryRun".Translate());
                GUI.color = Color.white;
            }
            else
            {
                Rect execBtn = new Rect(Padding, y, 220f, BtnHeight);
                GUI.color = new Color(1f, 0.6f, 0.4f);
                if (Widgets.ButtonText(execBtn, "RimMind.UI.AgentFlowLab.ExecuteMechanism".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "RimMind.UI.AgentFlowLab.ConfirmExecute".Translate(),
                        "Confirm".Translate(),
                        () =>
                        {
                            try
                            {
                                SetStepStatus(FlowLabStep.Execute, StepStatus.Active);
                                if (_lastWriteArgs == null)
                                {
                                    _lastError = "RimMind.UI.AgentFlowLab.ExecuteNoWriteArgs".Translate();
                                    SetStepStatus(FlowLabStep.Execute, StepStatus.Failed);
                                    return;
                                }

                                var mechanismRegistry = RimMindServiceLocator.Get<IGameMechanismRegistry>();
                                if (mechanismRegistry == null)
                                {
                                    _lastError = "RimMind.UI.AgentFlowLab.ExecuteNoRegistry".Translate();
                                    SetStepStatus(FlowLabStep.Execute, StepStatus.Failed);
                                    return;
                                }

                                var targetMech = mechanismRegistry.FindById(_lastWriteArgs.MechanismId);
                                if (targetMech != null)
                                {
                                    var result = ExecuteMappedMechanism(targetMech, _lastWriteArgs, _lastOperationType).Result;
                                    if (result.IsOk)
                                    {
                                        _lastError = $"Execute {_lastOperationType} ok: {result.Value}";
                                        SetStepStatus(FlowLabStep.Execute, StepStatus.Completed);
                                    }
                                    else
                                    {
                                        _lastError = result.Error.Message;
                                        SetStepStatus(FlowLabStep.Execute, StepStatus.Failed);
                                    }
                                }
                                else
                                {
                                    _lastError = $"Mechanism not found: {_lastWriteArgs.MechanismId}";
                                    SetStepStatus(FlowLabStep.Execute, StepStatus.Failed);
                                }
                            }
                            catch (Exception ex)
                            {
                                _lastError = $"ExecuteMechanism: {ex.Message}";
                                SetStepStatus(FlowLabStep.Execute, StepStatus.Failed);
                            }
                        },
                        "Cancel".Translate(),
                        null,
                        "RimMind.UI.AgentFlowLab.ExecuteMechanism".Translate()));
                }
                GUI.color = Color.white;
            }
            y += BtnHeight + Padding;

            if (!string.IsNullOrEmpty(_validationInfo))
            {
                GUI.color = new Color(1f, 0.7f, 0.3f);
                float h = Text.CalcHeight(_validationInfo, w - Padding * 2);
                h = Mathf.Min(h, 40f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, h), _validationInfo);
                GUI.color = Color.white;
                y += h + Padding;
            }

            y += SectionGap;
        }

        private void DrawQueueState(ref float y, float w)
        {
            DrawSectionLabel(ref y, w, "RimMind.UI.AgentFlowLab.QueueState");

            try
            {
                var queue = RimMindServiceLocator.Get<IAIRequestQueue>();
                if (queue != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Paused: {queue.IsPaused}  Active: {queue.ActiveRequestCount}  LocalBusy: {queue.IsLocalModelBusy}");

                    if (_selectedPawn != null)
                    {
                        string npcId = $"NPC-{_selectedPawn.thingIDNumber}";
                        var active = queue.GetActiveRequests();
                        var pawnRequests = active.Where(r => r.Envelope?.NpcId == npcId).ToList();
                        if (pawnRequests.Count > 0)
                        {
                            sb.AppendLine($"Requests for this pawn ({pawnRequests.Count}):");
                            foreach (var req in pawnRequests)
                                sb.AppendLine($"  {req.RequestId} state={req.State} attempt={req.AttemptCount}");
                        }
                    }

                    _queueInfo = sb.ToString();
                }
                else
                {
                    _queueInfo = "Queue not available";
                }
            }
            catch (Exception ex)
            {
                _queueInfo = $"Error: {ex.Message}";
            }

            if (!string.IsNullOrEmpty(_queueInfo))
            {
                float h = Text.CalcHeight(_queueInfo, w - Padding * 2);
                h = Mathf.Min(h, 60f);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2, h), _queueInfo);
                GUI.color = Color.white;
                y += h + Padding;
            }

            y += SectionGap;
        }

        private void DrawOpenLogs(ref float y, float w)
        {
            DrawSectionLabel(ref y, w, "RimMind.UI.AgentFlowLab.OpenLogs");

            float btnW = (w - Padding * 6) / 5f;
            float x = Padding;

            if (Widgets.ButtonText(new Rect(x, y, btnW, BtnHeight),
                "RimMind.UI.AgentFlowLab.OpenRequestLog".Translate()))
            {
                Find.WindowStack.Add(new Window_RequestLog());
            }
            x += btnW + Padding;

            if (Widgets.ButtonText(new Rect(x, y, btnW, BtnHeight),
                "RimMind.UI.AgentFlowLab.OpenToolCallDebug".Translate()))
            {
                Find.WindowStack.Add(new Window_ToolCallDebug());
            }
            x += btnW + Padding;

            if (Widgets.ButtonText(new Rect(x, y, btnW, BtnHeight),
                "RimMind.UI.AgentFlowLab.OpenMechanismStatus".Translate()))
            {
                Find.WindowStack.Add(new Window_MechanismStatus());
            }
            x += btnW + Padding;

            if (Widgets.ButtonText(new Rect(x, y, btnW, BtnHeight),
                "RimMind.UI.AgentFlowLab.OpenContextKeys".Translate()))
            {
                Find.WindowStack.Add(new Window_ContextKeyDebug());
            }
            x += btnW + Padding;

            if (Widgets.ButtonText(new Rect(x, y, btnW, BtnHeight),
                "RimMind.UI.AgentFlowLab.OpenAgentProgress".Translate()))
            {
                Find.WindowStack.Add(new Window_AgentProgressFloat());
            }
            y += BtnHeight + Padding;

            y += SectionGap;
        }

        private void DrawErrorLog(ref float y, float w)
        {
            DrawSectionLabel(ref y, w, "RimMind.UI.AgentFlowLab.ErrorLog");

            if (!string.IsNullOrEmpty(_lastError))
            {
                float h = Text.CalcHeight(_lastError, w - Padding * 2 - 80f);
                h = Mathf.Min(h, 40f);
                GUI.color = new Color(1f, 0.5f, 0.4f);
                Widgets.Label(new Rect(Padding, y, w - Padding * 2 - 80f, h), _lastError);
                GUI.color = Color.white;
                y += h + Padding;
            }
            else
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(Padding, y, w - Padding * 2 - 80f, LineH),
                    "RimMind.UI.AgentFlowLab.NoError".Translate());
                GUI.color = Color.white;
                y += LineH;
            }

            Rect clearBtn = new Rect(w - 80f, y - BtnHeight - Padding, 74f, BtnHeight);
            if (Widgets.ButtonText(clearBtn, "RimMind.UI.AgentFlowLab.ClearError".Translate()))
            {
                _lastError = "";
            }
        }

        private void PerformDryRun()
        {
            SetStepStatus(FlowLabStep.DryRun, StepStatus.Active);
            SetStepStatus(FlowLabStep.MapMechanism, StepStatus.Active);
            _dryRunCompleted = false;
            _lastWriteArgs = null;

            var mechanismRegistry = RimMindServiceLocator.Get<IGameMechanismRegistry>();
            var toolRegistry = RimMindServiceLocator.Get<IToolRegistry>();
            var approvalGate = RimMindServiceLocator.Get<IHumanApprovalGate>();

            if (mechanismRegistry == null)
            {
                _dryRunResult = "MechanismRegistry not available";
                _lastError = _dryRunResult;
                _lastWriteArgs = null;
                _lastOperationType = MechanismOperationType.Set;
                SetStepStatus(FlowLabStep.DryRun, StepStatus.Failed);
                SetStepStatus(FlowLabStep.MapMechanism, StepStatus.Failed);
                return;
            }

            var sb = new StringBuilder();
            var mechanisms = mechanismRegistry.All;
            sb.AppendLine("RimMind.UI.AgentFlowLab.DryRunHeader".Translate(mechanisms.Count.ToString()));

            foreach (var mech in mechanisms)
            {
                var writeActions = mech.GetWriteActions();
                string actionsStr = writeActions != null && writeActions.Count > 0
                    ? string.Join(", ", writeActions.Select(a => a.Action))
                    : "(read-only)";
                string toolMatch = "";
                if (toolRegistry != null)
                {
                    var match = toolRegistry.FindById(mech.MechanismId);
                    toolMatch = match != null ? $" -> tool:{match.Definition.Id}" : " -> no tool mapping";
                }

                string riskStr = mech.Risk.ToString();
                string approvalStr = "";
                if (approvalGate != null && _lastDecision != null)
                {
                    var riskLevel = mech.Risk switch
                    {
                        MechanismRisk.Safe => RiskLevel.Low,
                        MechanismRisk.Moderate => RiskLevel.Medium,
                        MechanismRisk.Dangerous => RiskLevel.High,
                        _ => RiskLevel.Low
                    };
                    bool needsApproval = approvalGate.RequiresApproval(_lastDecision, riskLevel);
                    approvalStr = needsApproval ? " [APPROVAL REQUIRED]" : "";
                }

                sb.AppendLine($"  {mech.MechanismId} [{mech.Scope}] risk={riskStr} actions={actionsStr}{toolMatch}{approvalStr}");
            }

            if (_lastDecision == null)
            {
                sb.AppendLine();
                sb.AppendLine("RimMind.UI.AgentFlowLab.DryRunNoDecision".Translate());
                _dryRunResult = sb.ToString();
                _lastError = "RimMind.UI.AgentFlowLab.DryRunNoDecision".Translate();
                _lastWriteArgs = null;
                _lastOperationType = MechanismOperationType.Set;
                SetStepStatus(FlowLabStep.DryRun, StepStatus.Failed);
                SetStepStatus(FlowLabStep.MapMechanism, StepStatus.Failed);
                return;
            }

            if (_lastDecision != null)
            {
                sb.AppendLine();
                sb.AppendLine("RimMind.UI.AgentFlowLab.DryRunDecision".Translate(_lastDecision.ActionIntent));
                var writeArgs = DecisionMapper.ToWriteArgs(_lastDecision, _selectedPawn?.thingIDNumber ?? 0);
                _lastOperationType = DecisionMapper.InferOperationType(writeArgs.Action);
                var targetMech = mechanismRegistry.FindById(writeArgs.MechanismId);
                if (targetMech != null)
                {
                    _lastWriteArgs = writeArgs;
                    sb.AppendLine("RimMind.UI.AgentFlowLab.DryRunTarget".Translate(
                        targetMech.MechanismId, targetMech.Risk.ToString()));
                    sb.AppendLine($"  operation={_lastOperationType} action={writeArgs.Action}");

                    if (targetMech.Risk == MechanismRisk.Dangerous)
                    {
                        sb.AppendLine("  *** DANGEROUS - Approval required before execution ***");
                    }
                }
                else
                {
                    string noTarget = "RimMind.UI.AgentFlowLab.DryRunNoTarget".Translate(writeArgs.MechanismId);
                    sb.AppendLine(noTarget);
                    _dryRunResult = sb.ToString();
                    _lastError = noTarget;
                    _lastWriteArgs = null;
                    _lastOperationType = MechanismOperationType.Set;
                    SetStepStatus(FlowLabStep.DryRun, StepStatus.Failed);
                    SetStepStatus(FlowLabStep.MapMechanism, StepStatus.Failed);
                    return;
                }

                if (toolRegistry != null)
                {
                    var validator = RimMindServiceLocator.Get<IDecisionValidator>();
                    if (validator != null)
                    {
                        var validationResult = validator.Validate(_lastDecision, toolRegistry);
                        _validationInfo = validationResult.IsValid
                            ? "RimMind.UI.AgentFlowLab.ValidationPassed".Translate()
                            : "RimMind.UI.AgentFlowLab.ValidationFailed".Translate(validationResult.Reason);
                    }
                }
            }

            _dryRunResult = sb.ToString();
            _dryRunCompleted = true;
            SetStepStatus(FlowLabStep.DryRun, StepStatus.Completed);
            SetStepStatus(FlowLabStep.MapMechanism, StepStatus.Completed);
        }

        private static Task<Result<bool, RimMindError>> ExecuteMappedMechanism(
            IGameMechanism mechanism,
            MechanismWriteArgs args,
            MechanismOperationType operationType)
        {
            return operationType switch
            {
                MechanismOperationType.Add => mechanism.ExecuteAddAsync(args, default),
                MechanismOperationType.Remove => mechanism.ExecuteRemoveAsync(args, default),
                MechanismOperationType.Toggle => mechanism.ExecuteToggleAsync(args, default),
                MechanismOperationType.Trigger => mechanism.ExecuteTriggerAsync(args, default),
                MechanismOperationType.Watch => mechanism.ExecuteWatchAsync(args, default),
                _ => mechanism.ExecuteSetAsync(args, default),
            };
        }

        private void AutoDryRun()
        {
            try
            {
                PerformDryRun();
            }
            catch (Exception ex)
            {
                _lastError = $"AutoDryRun: {ex.Message}";
                SetStepStatus(FlowLabStep.DryRun, StepStatus.Failed);
            }
        }

        private static string FormatDecision(AgentDecision decision)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ActionIntent: {decision.ActionIntent}");
            sb.AppendLine($"Reason: {decision.Reason}");
            if (!string.IsNullOrEmpty(decision.TargetPawnId))
                sb.AppendLine($"TargetPawnId: {decision.TargetPawnId}");
            if (!string.IsNullOrEmpty(decision.Param))
                sb.AppendLine($"Param: {decision.Param}");
            return sb.ToString();
        }
    }
}
