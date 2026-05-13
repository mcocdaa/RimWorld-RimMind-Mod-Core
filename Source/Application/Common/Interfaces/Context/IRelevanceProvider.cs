using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Context
{
    public interface IRelevanceProvider
    {
        float ComputeRelevance(string scenarioId, string npcId, KeyMeta key);
    }
}
