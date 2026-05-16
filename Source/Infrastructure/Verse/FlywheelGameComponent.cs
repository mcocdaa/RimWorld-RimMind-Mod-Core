using System;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Logging;
using RimMind.Application.Features.Flywheel;
using Verse;
using RimMind.Domain.ValueObjects;

namespace RimMind.Infrastructure.Verse
{
    public class FlywheelGameComponent : GameComponent
    {
        private int _lastAnalysisTick;

        private int AnalysisIntervalTicks =>
            RimMindServiceLocator.Get<ISettingsProvider>()?.ContextCalibrateInterval ?? 10000;

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
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                RimMindServiceLocator.Get<FlywheelTelemetryCollector>()?.Flush();
            }
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
            var telemetry = RimMindServiceLocator.Get<FlywheelTelemetryCollector>();
            var records = telemetry?.GetRecentRecords(100);
            if (records == null || records.Count == 0) return;
            FlywheelRuleEngine.Analyze(records);
        }
    }
}
