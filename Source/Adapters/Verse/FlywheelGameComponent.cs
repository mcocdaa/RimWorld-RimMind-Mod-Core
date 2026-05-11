using System;
using RimMind.Core;
using RimMind.Core.Runtime;
using RimMind.Kernel.Logging;
using RimMind.Kernel.Flywheel;
using Verse;
using RimMind.Contracts.Result;

namespace RimMind.Adapters.Verse
{
    public class FlywheelGameComponent : GameComponent
    {
        private int _lastAnalysisTick;

        private int AnalysisIntervalTicks =>
            RimMindCoreMod.Settings?.contextCalibrateInterval ?? 10000;

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
                RimMindRuntime.Instance.Telemetry.Flush();
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
            var records = RimMindRuntime.Instance.Telemetry.GetRecentRecords(100);
            if (records == null || records.Count == 0) return;
            FlywheelRuleEngine.Analyze(records);
        }
    }
}
