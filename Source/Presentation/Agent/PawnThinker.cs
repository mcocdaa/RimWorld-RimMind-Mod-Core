using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Domain.Enums;
using RimMind.Presentation;
using RimMind.Presentation.Runtime;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnThinker
    {
        private readonly IPawnAgent _agent;
        private int _lastThinkTick;
        private int _thinkCooldownTicks;
        private bool _thinking;

        public PawnThinker(IPawnAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _thinkCooldownTicks = RimMindServiceLocator.Get<IAgentTickSettings>()?.ThinkCooldownTicks ?? 30000;
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

                var strategy = mode.GetThinkStrategy();
                var allowedToolIds = mode.AllowedToolIds(RimMindAPI.Tools);

                var availableTools = RimMindAPI.Tools.GetAllDefinitions()
                    .Where(d => allowedToolIds.Contains(d.Id))
                    .ToList();

                var contextRequest = strategy.BuildRequest(_agent, entries, availableTools);
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
            catch
            {
                _thinking = false;
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
