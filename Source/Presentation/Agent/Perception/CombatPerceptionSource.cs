using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Perception;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Common;
using Verse;

namespace RimMind.Presentation.Agent.Perception
{
    public sealed class CombatPerceptionSource : IPerceptionSource
    {
        public string Id => "rimmind.perception.combat";
        public string OwnerModId => "RimMindCore";
        public int Priority => 30;

        public bool ShouldSense(IAgentInfo agent) => agent.State == Domain.Enums.AgentState.Active;

        public IReadOnlyList<PerceptionBufferEntry> Sense(IAgentInfo agent)
        {
            var entries = new List<PerceptionBufferEntry>();
            if (agent is IPawnAgent pawnAgent)
            {
                var pawn = pawnAgent.Pawn;
                if (pawn?.Drafted == true)
                {
                    entries.Add(new PerceptionBufferEntry
                    {
                        PerceptionType = "combat",
                        Content = "Currently drafted for combat",
                        Importance = RimMindDefaults.PerceptionCriticalThreshold,
                        Tick = Find.TickManager.TicksGame
                    });
                }
            }
            return entries;
        }
    }
}
