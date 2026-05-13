using RimMind.Application.Common.Models.Context;

namespace RimMind.Application.Common.Interfaces.Flywheel
{
    public interface IKernelParameterTuner
    {
        string TunerId { get; }
        void Tune(BudgetSchedulerConfig config);
    }
}
