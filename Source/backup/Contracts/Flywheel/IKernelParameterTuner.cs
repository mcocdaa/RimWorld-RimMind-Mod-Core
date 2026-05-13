using RimMind.Contracts.Context;

namespace RimMind.Contracts.Flywheel
{
    public interface IKernelParameterTuner
    {
        string TunerId { get; }
        void Tune(BudgetSchedulerConfig config);
    }
}
