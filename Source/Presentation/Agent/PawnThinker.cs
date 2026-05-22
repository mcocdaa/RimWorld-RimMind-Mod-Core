using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Features.Agent.InnerVoice;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Common.Models;
using RimMind.Domain.Enums;
using RimMind.Domain.Events;
using RimMind.Presentation;
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
        private int _lastThinkTick;
        private int _thinkCooldownTicks;
        private volatile bool _thinking;
        private InnerVoiceHandler? _innerVoiceHandler;

        private InnerVoiceHandler? GetInnerVoiceHandler()
            => _innerVoiceHandler ??= RimMindServiceLocator.Get<InnerVoiceHandler>();

        public PawnThinker(IPawnAgent agent, IAgentTickSettings tickSettings, IAgentBus agentBus)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _tickSettings = tickSettings;
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
            _thinkCooldownTicks = _tickSettings?.ThinkCooldownTicks ?? DefaultThinkCooldownTicks;
        }

        public bool IsThinking => _thinking;
        public int LastThinkTick => _lastThinkTick;

        public void Tick()
        {
            if (_agent.State != AgentState.Active) return;
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
                var mode = _agent.CurrentMode;

                if (!mode.ShouldThink(_agent, entries)) { _thinking = false; return; }

                // InnerVoice context injection
                var innerVoice = GetInnerVoiceHandler();
                var voiceText = innerVoice?.GetPendingVoiceText(_agent.Identity.NpcId);
                if (!string.IsNullOrEmpty(voiceText))
                {
                    innerVoice?.ClearVoice(_agent.Identity.NpcId);
                }

                // Reflection and planning triggers (Proactive mode only)
                if (mode is ProactiveAgentMode proactiveMode)
                {
                    if (proactiveMode.ReflectionStrategy?.ShouldReflect(_agent) == true)
                    {
                        try
                        {
                            var reflectionResult = proactiveMode.ReflectionStrategy.ReflectAsync(_agent).Result;
                            if (reflectionResult.IsOk && reflectionResult.Value.Count > 0)
                            {
                                Log.Message($"[RimMind] Reflection: {_agent.Identity.NpcId} generated {reflectionResult.Value.Count} insights");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"[Think] Reflection failed for {_agent.Identity.NpcId}: {ex.Message}");
                        }
                    }

                    if (proactiveMode.DailyPlanner?.ShouldPlan(_agent) == true)
                    {
                        try
                        {
                            var planResult = proactiveMode.DailyPlanner.PlanAsync(_agent).Result;
                            if (planResult.IsOk && planResult.Value.Count > 0)
                            {
                                Log.Message($"[RimMind] Planning: {_agent.Identity.NpcId} generated {planResult.Value.Count} schedule blocks");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"[Think] Planning failed for {_agent.Identity.NpcId}: {ex.Message}");
                        }
                    }
                }

                var strategy = mode.GetThinkStrategy();
                var allowedToolIds = mode.AllowedToolIds(RimMindAPI.Tools);

                var availableTools = RimMindAPI.Tools.GetAllDefinitions()
                    .Where(d => allowedToolIds.Contains(d.Id))
                    .ToList();

                var contextRequest = strategy.BuildRequest(_agent, entries, availableTools);

                // Inject InnerVoice as high-priority context
                if (!string.IsNullOrEmpty(voiceText) && contextRequest != null)
                {
                    contextRequest.CurrentQuery = $"[Inner Voice: {voiceText}]\n{contextRequest.CurrentQuery}";
                }

                var structuredTools = ConvertToStructuredTools(availableTools);

                RimMindAPI.Request.RequestStructured(contextRequest, "<Action>...</Action>", result =>
                {
                    _thinking = false;
                    if (result.IsOk)
                    {
                        var decision = strategy.ParseDecision(_agent, result.Value);
                        if (decision.IsOk)
                        {
                            _agent.LastThinkTick = Find.TickManager.TicksGame;
                            _agent.RecordBehavior(new BehaviorRecordDto
                            {
                                Action = decision.Value.ActionIntent,
                                Reason = decision.Value.Reason,
                                Success = true,
                                Timestamp = Find.TickManager.TicksGame
                            });

                            // Publish DecisionEvent for Flywheel decision tracking
                            _agentBus.Publish(new DecisionEvent(
                                _agent.Identity.NpcId,
                                _agent.Pawn?.thingIDNumber ?? -1,
                                decision.Value.ActionIntent ?? "think",
                                decision.Value.Reason ?? "",
                                decision.Value.ActionIntent ?? ""));
                        }
                        else
                        {
                            Log.Warning($"[Think] Parse failed: {decision.Error}");
                        }
                    }
                    else
                    {
                        Log.Warning($"[Think] AI request failed: {result.Error}");
                    }
                }, structuredTools);
            }
            catch (Exception ex)
            {
                _thinking = false;
                Log.Error($"[Think] Unexpected error for {_agent.Identity.NpcId}: {ex}");
            }
        }

        private static List<StructuredTool> ConvertToStructuredTools(IReadOnlyList<ToolDefinition> defs)
        {
            return defs.Select(d => new StructuredTool
            {
                Name = d.Id,
                Description = d.Description,
                Parameters = d.ParametersSchema,
            }).ToList();
        }

        public void ForceThink()
        {
            _lastThinkTick = 0;
        }
    }
}
