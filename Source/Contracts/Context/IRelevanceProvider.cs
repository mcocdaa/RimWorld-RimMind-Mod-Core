namespace RimMind.Contracts.Context
{
    public interface IRelevanceProvider
    {
        float ComputeRelevance(string scenarioId, string npcId, KeyMeta key);
    }
}
