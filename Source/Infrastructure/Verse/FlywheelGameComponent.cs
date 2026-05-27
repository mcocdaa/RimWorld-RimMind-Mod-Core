using System;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Flywheel;
using RimMind.Domain.ValueObjects;

using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class FlywheelGameComponent : GameComponent
    {
        private int _lastAnalysisTick;
        private IContextCalibrationSettings? _calibrationSettings;
        private ITelemetryCollector? _telemetryCollector;
        private IFlywheelRuleEngine? _ruleEngine;

        // [Framework-Forced SL] Verse GameComponent requires parameterless constructor.
        // EnsureCached() guard pattern: resolves once on first access, then uses cached fields.
        private void EnsureCached()
        {
            if (_calibrationSettings != null) return;
            _calibrationSettings = RimMindServiceLocator.Get<IContextCalibrationSettings>();
            _telemetryCollector = RimMindServiceLocator.Get<ITelemetryCollector>();
            _ruleEngine = RimMindServiceLocator.Get<IFlywheelRuleEngine>();
        }

        private int AnalysisIntervalTicks
        {
            get
            {
                EnsureCached();
                return _calibrationSettings?.ContextCalibrateInterval ?? RimMindDefaults.FlywheelCalibrateInterval;
            }
        }

        public FlywheelGameComponent() : base() { }
        public FlywheelGameComponent(Game game) : base() { }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            _lastAnalysisTick = 0;
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            _lastAnalysisTick = 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            int ticks = Find.TickManager?.TicksGame ?? 0;
            if (_lastAnalysisTick == 0)
                _lastAnalysisTick = ticks;
            if (ticks - _lastAnalysisTick >= AnalysisIntervalTicks)
            {
                _lastAnalysisTick = ticks;
                try
                {
                    RunPeriodicAnalysis();
                }
                catch (Exception ex) { RimMindErrors.Warn($"[RimMind] Flywheel analysis failed: {ex.Message}"); }
            }
        }

        private void RunPeriodicAnalysis()
        {
            EnsureCached();
            var records = _telemetryCollector?.GetRecentRecords(RimMindDefaults.TelemetryRecordLimit);
            if (records == null || records.Count == 0) return;
            _ruleEngine?.Analyze(records);
        }
    }
}
