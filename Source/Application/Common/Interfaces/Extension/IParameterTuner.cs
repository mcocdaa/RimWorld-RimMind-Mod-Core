using System.Collections.Generic;
using RimMind.Application.Common.Models.Client;

namespace RimMind.Application.Common.Interfaces.Extension
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
