using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Flywheel;

namespace RimMind.Application.Features.Flywheel
{
    public sealed class FlywheelParameterStore : IFlywheelParameterStore
    {
        private readonly ConcurrentDictionary<string, float> _parameters
            = new ConcurrentDictionary<string, float>();
        private readonly ConcurrentDictionary<string, float> _defaults
            = new ConcurrentDictionary<string, float>();
        private readonly ILogSink? _log;

        public event Action<string, float>? OnParameterChanged;

        public FlywheelParameterStore(ILogSink? log = null)
        {
            _log = log;
            InitializeDefaults();
        }

        public int TotalBudget => 0;

        public float Get(string key)
        {
            return _parameters.TryGetValue(key, out var val) ? val : 0f;
        }

        public void UpdateParameter(string key, float value)
        {
            _parameters[key] = value;
            OnParameterChanged?.Invoke(key, value);
        }

        public void ResetToDefault(string key)
        {
            if (_defaults.TryGetValue(key, out var def))
            {
                _parameters[key] = def;
                OnParameterChanged?.Invoke(key, def);
            }
        }

        public void ResetAll()
        {
            foreach (var kvp in _defaults)
            {
                _parameters[kvp.Key] = kvp.Value;
                OnParameterChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }

        public Dictionary<string, float> GetAll()
        {
            return new Dictionary<string, float>(_parameters);
        }

        public Dictionary<string, float> GetDefaults()
        {
            return new Dictionary<string, float>(_defaults);
        }

        private void InitializeDefaults()
        {
            _defaults["ContextBudget"] = 1.0f;
            _defaults["MaxCacheEntries"] = 200f;
            _defaults["W1"] = 0.4f;
            _defaults["W2"] = 0.6f;
            _defaults["Alpha"] = 0.01f;
            _defaults["AlphaSmooth"] = 0.7f;
            _defaults["PromoteThreshold"] = 0.8f;
            _defaults["DemoteThreshold"] = 0.2f;
            foreach (var kvp in _defaults)
                _parameters[kvp.Key] = kvp.Value;
        }
    }
}
