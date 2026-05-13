using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;

namespace RimMind.Application.Common.Interfaces.Runtime
{
    public interface IRimMindRuntime
    {
        bool IsShutdown { get; }
        void RegisterParameterTuner(IKernelParameterTuner tuner);
        IReadOnlyList<IKernelParameterTuner> ParameterTunersList { get; }
        IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension;
    }
}
