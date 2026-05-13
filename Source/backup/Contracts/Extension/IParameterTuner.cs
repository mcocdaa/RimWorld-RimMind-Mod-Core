using System.Collections.Generic;
using RimMind.Contracts.Client;

namespace RimMind.Contracts.Extension
{
    public interface IParameterTuner
    {
        string Name { get; }
        string TunerId { get; }
        float TuneParameter(string parameterName, float currentValue);
        bool ShouldApply(string npcId);
        void Tune(object config);
    }
}
