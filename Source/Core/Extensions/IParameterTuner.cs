using RimMind.Contracts.Context;

namespace RimMind.Core.Extensions
{
    public interface IParameterTuner
    {
        string TunerId { get; }
        void Tune(BudgetSchedulerConfig config);
    }
}
