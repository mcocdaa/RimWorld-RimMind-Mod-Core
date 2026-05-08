using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts;
using RimMind.Contracts.Flywheel;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;
using RimMind.Kernel.Logging;

namespace RimMind.Kernel.Flywheel
{
    public class FlywheelParameterStore : IFlywheelParameterStore
    {
        public static IFlywheelParameterStore? Instance
        {
            get => RimMindServiceLocator.Get<IFlywheelParameterStore>();
            private set
            {
                if (value != null)
                    RimMindServiceLocator.Register<IFlywheelParameterStore>(value);
            }
        }

        private readonly ConcurrentDictionary<string, float> _parameters = new ConcurrentDictionary<string, float>();
        private readonly ConcurrentDictionary<string, float> _defaults = new ConcurrentDictionary<string, float>();

        public event Action<string, float>? OnParameterChanged;

        public FlywheelParameterStore()
        {
            RegisterDefaults();
        }

        public int TotalBudget => (int)Get("TotalBudget");

        private void RegisterDefaults()
        {
            _defaults["w1"] = 0.4f;
            _defaults["w2"] = 0.6f;
            _defaults["Alpha"] = 0.01f;
            _defaults["AlphaSmooth"] = 0.7f;
            _defaults["PromoteThreshold"] = 0.8f;
            _defaults["DemoteThreshold"] = 0.2f;
            _defaults["Dialogue.Budget"] = 0.6f;
            _defaults["Decision.Budget"] = 0.5f;
            _defaults["Personality.Budget"] = 0.4f;
            _defaults["Storyteller.Budget"] = 0.7f;
            _defaults["DiffMergeRounds"] = 4f;
            _defaults["TotalBudget"] = 4000f;
            _defaults["ReserveForOutput"] = 800f;
            _defaults["MaxHistoryRounds"] = 6f;

            foreach (var kvp in _defaults)
            {
                if (!_parameters.ContainsKey(kvp.Key))
                    _parameters[kvp.Key] = kvp.Value;
            }
        }

        public float Get(string key)
        {
            return _parameters.TryGetValue(key, out var val) ? val : (_defaults.TryGetValue(key, out var def) ? def : 0f);
        }

        public void UpdateParameter(string key, float value)
        {
            value = ClampParameter(key, value);
            float old = Get(key);
            _parameters[key] = value;
            if (Math.Abs(old - value) > 0.0001f)
            {
                var handler = OnParameterChanged;
                handler?.Invoke(key, value);
                if (RimMindModAccessor.Settings?.debugLogging == true)
                    RimMindLogger.Message($"FlywheelParameterStore: {key} = {value} (was {old})");
            }
        }

        public void ResetToDefault(string key)
        {
            if (_defaults.TryGetValue(key, out var def))
                UpdateParameter(key, def);
        }

        public void ResetAll()
        {
            foreach (var kvp in _defaults)
                UpdateParameter(kvp.Key, kvp.Value);
        }

        public Dictionary<string, float> GetAll()
        {
            return new Dictionary<string, float>(_parameters);
        }

        public Dictionary<string, float> GetDefaults()
        {
            return new Dictionary<string, float>(_defaults);
        }

        internal void LoadFromSnapshot(List<string> keys, List<float> values)
        {
            if (keys != null && values != null && keys.Count == values.Count)
            {
                for (int i = 0; i < keys.Count; i++)
                    _parameters[keys[i]] = values[i];
            }
        }

        internal (List<string> keys, List<float> values) GetSaveSnapshot()
        {
            var snapshot = _parameters.ToArray();
            var keys = new List<string>(snapshot.Select(kvp => kvp.Key));
            var values = new List<float>(snapshot.Select(kvp => kvp.Value));
            return (keys, values);
        }

        private static float ClampParameter(string key, float value)
        {
            switch (key)
            {
                case "Alpha": return Math.Clamp(value, 0.0f, 1.0f);
                case "PromoteThreshold": return Math.Clamp(value, 0.0f, 1.0f);
                case "TotalBudget": return Math.Clamp(value, 100f, 32000f);
                case "DecayRate": return Math.Clamp(value, 0.0f, 1.0f);
                default: return value;
            }
        }

        public void FinalizeInit()
        {
            Instance = this;
            RegisterDefaults();
        }
    }
}
