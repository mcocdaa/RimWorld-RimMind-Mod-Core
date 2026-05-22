using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Enums;
using RimMind.Domain.Events;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Extension;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnPerceiver : IPawnPerceiver
    {
        private const int DefaultPerceptionInterval = RimMindDefaults.AgentTickInterval;
        private const float MoodImportance = RimMindDefaults.PerceptionLowThreshold;
        private const float HediffSeverityThreshold = RimMindDefaults.PerceptionMediumThreshold;
        private const float HealthImportance = RimMindDefaults.PerceptionHighThreshold;
        private const float CombatImportance = RimMindDefaults.PerceptionCriticalThreshold;

        private readonly IPawnAgent _agent;
        private readonly IAgentBus _agentBus;
        private int _lastPerceptionTick;
        private int _perceptionInterval = DefaultPerceptionInterval;

        public PawnPerceiver(IPawnAgent agent, IAgentBus agentBus)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
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
                    Importance = MoodImportance,
                    Tick = Find.TickManager.TicksGame
                });
            }

            if (pawn.health?.hediffSet?.hediffs != null)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff != null && hediff.Visible && hediff.Severity > HediffSeverityThreshold)
                    {
                        _agent.PerceptionBuffer.Add(new PerceptionBufferEntry
                        {
                            PerceptionType = "health",
                            Content = $"Health issue: {hediff.Label}",
                            Importance = HealthImportance,
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
                    Importance = CombatImportance,
                    Tick = Find.TickManager.TicksGame
                });
            }

            // Publish PerceptionEvent so subscribers can react to perception changes
            _agentBus.Publish(new PerceptionEvent(
                _agent.Identity.NpcId,
                _agent.Pawn?.thingIDNumber ?? -1,
                "composite",
                "Perception cycle completed",
                0f,
                Find.TickManager.TicksGame));
        }
    }
}
