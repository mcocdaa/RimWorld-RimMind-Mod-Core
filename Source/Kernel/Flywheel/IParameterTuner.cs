using RimMind.Kernel.Context;

namespace RimMind.Kernel.Flywheel
{
    public interface IKernelParameterTuner
    {
        string TunerId { get; }
        void Tune(BudgetSchedulerConfig config);
    }
}
