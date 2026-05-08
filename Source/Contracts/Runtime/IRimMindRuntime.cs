using System;
using System.Collections.Generic;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Flywheel;

namespace RimMind.Contracts.Runtime
{
    public interface IRimMindRuntime
    {
        void RegisterParameterTuner(IKernelParameterTuner tuner);
        IReadOnlyList<IKernelParameterTuner> ParameterTunersList { get; }
        IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension;
    }
}
