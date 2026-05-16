using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Enums;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnPerceiver
    {
        private readonly IPawnAgent _agent;
        private int _lastPerceptionTick;
        private int _perceptionInterval = 150;

        public PawnPerceiver(IPawnAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public void Tick()
        {
            if (_agent.State != AgentState.Active) return;
            if (Find.TickManager.TicksGame - _lastPerceptionTick < _perceptionInterval) return;
            _lastPerceptionTick = Find.TickManager.TicksGame;
            Sense();
        }

        private void Sense()
        {
            var pawn = _agent.Pawn;
            if (pawn == null || pawn.Dead) return;

            if (pawn.needs?.mood != null)
            {
                _agent.PerceptionBuffer.Add(new PerceptionBufferEntry
                {
                    PerceptionType = "mood",
                    Content = $"Mood: {pawn.needs.mood.CurLevel:P0}",
                    Importance = 0.3f,
                    Tick = Find.TickManager.TicksGame
                });
            }

            if (pawn.health?.hediffSet?.hediffs != null)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff != null && hediff.Visible && hediff.Severity > 0.5f)
                    {
                        _agent.PerceptionBuffer.Add(new PerceptionBufferEntry
                        {
                            PerceptionType = "health",
                            Content = $"Health issue: {hediff.Label}",
                            Importance = 0.7f,
                            Tick = Find.TickManager.TicksGame
                        });
                    }
                }
            }

            if (pawn.Drafted)
            {
                _agent.PerceptionBuffer.Add(new PerceptionBufferEntry
                {
                    PerceptionType = "combat",
                    Content = "Currently drafted for combat",
                    Importance = 0.8f,
                    Tick = Find.TickManager.TicksGame
                });
            }
        }
    }
}
