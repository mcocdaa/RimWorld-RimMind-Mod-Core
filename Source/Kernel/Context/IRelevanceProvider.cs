namespace RimMind.Kernel.Context
{
    public interface IRelevanceProvider
    {
        float ComputeRelevance(string scenarioId, string npcId, KeyMeta key);
    }
}
