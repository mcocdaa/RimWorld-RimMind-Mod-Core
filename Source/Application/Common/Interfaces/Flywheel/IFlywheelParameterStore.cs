using System;
using System.Collections.Generic;
using RimMind.Application.Common.Models.Context;

namespace RimMind.Application.Common.Interfaces.Flywheel
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
        (List<string> keys, List<float> values) GetSaveSnapshot();
        void LoadFromSnapshot(List<string> keys, List<float> values);
        void FinalizeInit();
        event Action<string, float>? OnParameterChanged;
    }
}
