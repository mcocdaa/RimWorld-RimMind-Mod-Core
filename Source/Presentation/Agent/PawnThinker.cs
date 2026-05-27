using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Common.Models;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Common;
using RimMind.Domain.Enums;
using RimMind.Domain.Events;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Runtime;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnThinker : IPawnThinker
    {
        private const int DefaultThinkCooldownTicks = RimMindDefaults.ThinkCooldownTicks;

        private readonly IPawnAgent _agent;
        private readonly IAgentBus _agentBus;
        private readonly IAgentTickSettings? _tickSettings;
        private readonly ProactiveBehaviorExecutor _proactiveExecutor;
        private readonly ThinkContextEnricher _contextEnricher;
        private int _lastThinkTick;
        private int _thinkCooldownTicks;
        private volatile bool _thinking;
        private IReadOnlyList<PerceptionBufferEntry> _cachedPerceptions = Array.Empty<PerceptionBufferEntry>();
        private int _requestSentTick;

        // Main-thread callback dispatch: AI callback stores result, Tick() processes on main thread
        private volatile bool _hasPendingCallback;
        private Result<LlmResponse, RimMindError> _pendingResult;
        private LlmRequestContext? _pendingContext;
        private IThinkStrategy? _pendingStrategy;
        private IReadOnlyList<ToolDefinition>? _pendingAvailableTools;
        private int _pendingToolCallRound;
        private string? _pendingTraceId;

        public PawnThinker(IPawnAgent agent, IAgentTickSettings tickSettings, IAgentBus agentBus)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _tickSettings = tickSettings;
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
            _thinkCooldownTicks = _tickSettings?.ThinkCooldownTicks ?? DefaultThinkCooldownTicks;
            _proactiveExecutor = new ProactiveBehaviorExecutor(agentBus);
            _contextEnricher = new ThinkContextEnricher();
        }

        public bool IsThinking => _thinking;
        public int LastThinkTick => _lastThinkTick;

        public bool ShouldThink()
        {
            if (_agent.State != AgentState.Active) return false;
            if (_thinking) return false;
            if (Find.TickManager.TicksGame - _lastThinkTick < _thinkCooldownTicks) return false;
            return true;
        }

        public void ResetThinking()
        {
            _thinking = false;
            _hasPendingCallback = false;
            _requestSentTick = 0;
            _cachedPerceptions = Array.Empty<PerceptionBufferEntry>();
            _pendingTraceId = null;
        }

        public void Tick()
        {
            if (_agent.State != AgentState.Active) return;

            // Process pending AI callback on the main thread (thread-safe RimWorld object access)
            if (_hasPendingCallback)
            {
                _hasPendingCallback = false;
                ProcessPendingCallback();
            }

            // Timeout guard: if thinking has been stuck for too long, reset
            if (_thinking && _requestSentTick > 0)
            {
                var elapsed = Find.TickManager.TicksGame - _requestSentTick;
                if (elapsed > RimMindDefaults.ThinkRequestTimeoutTicks)
                {
                    Log.Warning($"[RimMind] Think request timeout for {_agent.Identity.NpcId} after {elapsed} ticks, resetting");
                    _thinking = false;
                    _requestSentTick = 0;
                    _cachedPerceptions = Array.Empty<PerceptionBufferEntry>();
                }
            }

            if (_thinking) return;
            if (Find.TickManager.TicksGame - _lastThinkTick < _thinkCooldownTicks) return;
            _lastThinkTick = Find.TickManager.TicksGame;
            Think();
        }

        private void Think()
        {
            _thinking = true;
            try
            {
                var pawn = _agent.Pawn;
                if (pawn == null || pawn.Dead) { _thinking = false; return; }

                var entries = _agent.PerceptionBuffer.Flush();
                _cachedPerceptions = entries;
                var mode = _agent.CurrentMode;

                if (!mode.ShouldThink(_agent, entries)) { _thinking = false; return; }

                var pawnId = pawn.thingIDNumber;

                // Pre-think enrichment: InnerVoice + Psychology
                var voiceText = _contextEnricher.ConsumeInnerVoice(_agent.Identity.NpcId);
                _contextEnricher.CheckPsychology(_agent, pawnId);

                // Proactive extension behaviors (Reflection/Planning/Dream/TraitEvolution)
                _proactiveExecutor.ExecuteProactiveExtensions(_agent, mode, pawnId);

                // Core think: strategy -> envelope -> AI -> decision
                var strategy = mode.GetThinkStrategy();
                var allowedToolIds = mode.AllowedToolIds(RimMindAPI.Tools);
                var availableTools = RimMindAPI.Tools.GetAllDefinitions()
                    .Where(d => allowedToolIds.Contains(d.Id))
                    .ToList();

                var envelope = strategy.BuildEnvelope(_agent, entries, availableTools);

                // Inject context enrichments
                _contextEnricher.EnrichEnvelope(envelope, _agent.Identity.NpcId, voiceText);

                // Inject behavior history feedback
                var recentHistory = _agent.GetRecentHistory(10);
                var successRate = _agent.GetRecentSuccessRate(10);
                var historySection = _contextEnricher.FormatBehaviorHistory(recentHistory, successRate);
                if (!string.IsNullOrEmpty(historySection))
                {
                    if (!string.IsNullOrEmpty(envelope.GameStateInfo))
                        envelope.GameStateInfo = historySection + envelope.GameStateInfo;
                    else
                        envelope.GameStateInfo = historySection.TrimEnd('\n');
                }

                _requestSentTick = Find.TickManager.TicksGame;
                SendThinkRequest(envelope, strategy, availableTools, 0);
            }
            catch (Exception ex)
            {
                _thinking = false;
                Log.Error($"[Think] Unexpected error for {_agent.Identity.NpcId}: {ex}");
            }
        }

        /// <summary>
        /// Sends a think request through the pipeline. The AI callback stores the result
        /// into pending fields for main-thread processing via ProcessPendingCallback().
        /// </summary>
        private void SendThinkRequest(
            LlmRequestEnvelope envelope,
            IThinkStrategy strategy,
            IReadOnlyList<ToolDefinition> availableTools,
            int toolCallRound)
        {
            // Capture strategy context for the callback (avoids closure over mutable state)
            _pendingStrategy = strategy;
            _pendingAvailableTools = availableTools;
            _pendingToolCallRound = toolCallRound;
            _pendingTraceId = envelope.TraceId;

            RimMindAPI.Request.Send(envelope, (result, ctx) =>
            {
                // Store result for main-thread processing — do NOT access RimWorld objects here
                _pendingResult = result;
                _pendingContext = ctx;
                _hasPendingCallback = true;
            });
        }

        /// <summary>
        /// Processes the pending AI callback result on the main thread.
        /// All RimWorld object access happens here, ensuring thread safety.
        /// </summary>
        private void ProcessPendingCallback()
        {
            var traceScope = _pendingTraceId != null
                ? TraceContext.BeginScope(_pendingTraceId)
                : null;
            try
            {
                var result = _pendingResult;
                var ctx = _pendingContext;
                var strategy = _pendingStrategy!;
                var availableTools = _pendingAvailableTools!;
                var toolCallRound = _pendingToolCallRound;

                if (!result.IsOk)
                {
                    _thinking = false;
                    _agent.TransitionWorkflow(AgentWorkflowPhase.Idle);
                    Log.Warning($"[Think] AI request failed: {result.Error}");
                    return;
                }

                var response = result.Value;
                var toolCallResults = ctx?.ToolCallResults;

                var decision = strategy.ParseDecision(_agent, response, toolCallResults);
                if (!decision.IsOk)
                {
                    _thinking = false;
                    _agent.TransitionWorkflow(AgentWorkflowPhase.Idle);
                    Log.Warning($"[Think] Parse failed: {decision.Error}");
                    return;
                }

                // Agentic loop: if AI wants more tool calls and depth not exceeded,
                // build a follow-up envelope with ToolCall results and send again
                if (decision.Value.WantsMoreToolCalls
                    && toolCallResults != null
                    && toolCallResults.Count > 0
                    && toolCallRound + 1 < RimMindDefaults.DefaultMaxToolCallDepth)
                {
                    var followUpEnvelope = strategy.BuildEnvelope(_agent, _cachedPerceptions, availableTools);
                    _contextEnricher.EnrichWithToolCallResults(followUpEnvelope, toolCallResults, toolCallRound + 1);
                    _contextEnricher.EnrichEnvelope(followUpEnvelope, _agent.Identity.NpcId, null);

                    SendThinkRequest(followUpEnvelope, strategy, availableTools, toolCallRound + 1);
                    return;
                }

                // Final decision: execute, record and publish
                _thinking = false;
                _cachedPerceptions = Array.Empty<PerceptionBufferEntry>();
                _agent.LastThinkTick = Find.TickManager.TicksGame;

                // Transition workflow: Thinking → Acting
                _agent.TransitionWorkflow(AgentWorkflowPhase.Acting);

                // Execute the decision via PawnActor's IActionExecutor
                var execResult = _agent.ExecuteDecision(decision.Value);
                var execSuccess = execResult.IsOk;

                // Transition workflow: Acting → Recording
                _agent.TransitionWorkflow(AgentWorkflowPhase.Recording);

                _agent.RecordBehavior(new BehaviorRecordDto
                {
                    Action = decision.Value.ActionIntent,
                    Reason = decision.Value.Reason,
                    Success = execSuccess,
                    Timestamp = Find.TickManager.TicksGame
                });
                _agentBus.Publish(new DecisionEvent(
                    _agent.Identity.NpcId,
                    _agent.Pawn?.thingIDNumber ?? -1,
                    decision.Value.ActionIntent ?? "think",
                    decision.Value.Reason ?? "",
                    decision.Value.ActionIntent ?? ""));

                // Transition workflow: Recording → Idle
                _agent.TransitionWorkflow(AgentWorkflowPhase.Idle);
            }
            catch (Exception ex)
            {
                _thinking = false;
                _agent.TransitionWorkflow(AgentWorkflowPhase.Idle);
                Log.Error($"[Think] Error processing AI callback for {_agent.Identity.NpcId}: {ex}");
            }
            finally
            {
                traceScope?.Dispose();
            }
        }

        public void ForceThink()
        {
            _lastThinkTick = 0;
        }
    }
}
