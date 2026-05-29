using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Perception;
using RimMind.Application.Common.Models.Pipeline;
using Verse;

namespace RimMind.Presentation.Agent.Perception
{
    public sealed class EnvironmentPerceptionSource : IPerceptionSource
    {
        public string Id => "rimmind.perception.environment";
        public string OwnerModId => "RimMindCore";
        public int Priority => 60;

        public bool ShouldSense(IAgentInfo agent) => agent.State == Domain.Enums.AgentState.Active;

        public IReadOnlyList<PerceptionBufferEntry> Sense(IAgentInfo agent)
        {
            var entries = new List<PerceptionBufferEntry>();
            if (agent is IPawnAgent pawnAgent)
            {
                var pawn = pawnAgent.Pawn;
                var map = pawn?.Map;
                if (map != null && pawn != null)
                {
                    var weather = map.weatherManager?.curWeather;
                    var temperature = GenTemperature.GetTemperatureForCell(pawn.Position, map);

                    bool isExtremeWeather = weather != null && weather.defName != "Clear";
                    bool isExtremeTemp = temperature < -10f || temperature > 45f;

                    if (isExtremeWeather || isExtremeTemp)
                    {
                        var weatherLabel = weather?.label ?? "unknown";
                        entries.Add(new PerceptionBufferEntry
                        {
                            PerceptionType = "environment",
                            Content = $"Environment: {weatherLabel}, {temperature:F0}\u00B0C",
                            Importance = 0.6f,
                            Tick = Find.TickManager.TicksGame
                        });
                    }
                }
            }
            return entries;
        }
    }
}
