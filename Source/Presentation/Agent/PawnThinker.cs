using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
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
            _thinkCooldownTicks = RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000;
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
                if (pawn == null || pawn.Dead) return;

                var entries = _agent.PerceptionBuffer.Flush();
                if (entries.Count == 0) return;

                var request = BuildRequest(pawn, entries);
                if (request == null) return;

                var client = RimMindRuntime.Instance.GetClient();
                if (client == null) return;

                RimMindAPI.Request.RequestStructuredAsync(request, null, response =>
                {
                    _thinking = false;
                    if (response.IsOk)
                        ProcessResponse(response.Value);
                });
            }
            catch
            {
                _thinking = false;
            }
        }

        private AIRequest? BuildRequest(Pawn pawn, List<PerceptionBufferEntry> entries)
        {
            var request = new AIRequest
            {
                SystemPrompt = "",
                MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
                RequestId = $"Think_{pawn.thingIDNumber}_{Find.TickManager.TicksGame}",
                ModId = "AgentThink",
                Priority = AIRequestPriority.Normal,
                UseJsonMode = RimMindCoreMod.Settings?.forceJsonMode ?? true,
            };
            return request;
        }

        private void ProcessResponse(AIResponse response)
        {
            if (response == null || string.IsNullOrEmpty(response.Content)) return;
            try
            {
                var action = ParseAction(response.Content);
                if (action != null)
                {
                    _agent.RecordBehavior(new BehaviorRecord
                    {
                        Action = action.Action,
                        Reason = action.Reason,
                        Success = true,
                        Timestamp = Find.TickManager.TicksGame
                    });
                }
            }
            catch { }
        }

        private ParsedAction? ParseAction(string content)
        {
            return null;
        }

        public void ForceThink()
        {
            _lastThinkTick = 0;
        }

        private class ParsedAction
        {
            public string Action = "";
            public string Reason = "";
        }
    }
}
