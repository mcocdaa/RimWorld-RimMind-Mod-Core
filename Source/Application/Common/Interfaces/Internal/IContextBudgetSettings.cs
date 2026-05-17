namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IContextBudgetSettings
    {
        float ContextBudget { get; set; }
        int ContextBriefLimit { get; }
        int MaxCacheEntries { get; }
        float BudgetW1 { get; set; }
        float BudgetW2 { get; set; }
    }
}
