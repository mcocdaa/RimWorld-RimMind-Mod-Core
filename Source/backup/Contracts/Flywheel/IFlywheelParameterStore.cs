using System;
using System.Collections.Generic;

namespace RimMind.Contracts.Flywheel
{
    public interface IFlywheelParameterStore
    {
        int TotalBudget { get; }
        float Get(string key);
        void UpdateParameter(string key, float value);
        void ResetToDefault(string key);
        void ResetAll();
        Dictionary<string, float> GetAll();
        Dictionary<string, float> GetDefaults();
        event Action<string, float>? OnParameterChanged;
    }
}
