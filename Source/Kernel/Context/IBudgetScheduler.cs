using System.Collections.Generic;

namespace RimMind.Kernel.Context
{
    public interface IBudgetScheduler
    {
        BudgetAllocation Schedule(List<KeyMeta> keys, string scenarioId, float budget, string? currentQuery);
        void OnKeyUpdated(KeyMeta key);
        void Calibrate(List<KeyMeta> keys);
        void SetRelevanceProvider(IRelevanceProvider provider);
        void SetConfig(BudgetSchedulerConfig? config);
        BudgetSchedulerConfig GetConfig();
    }
}
