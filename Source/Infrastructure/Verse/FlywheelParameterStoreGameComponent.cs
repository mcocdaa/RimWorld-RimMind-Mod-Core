using System.Collections.Generic;
using RimMind.Application.Features.Flywheel;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class FlywheelParameterStoreGameComponent : GameComponent
    {
        private readonly FlywheelParameterStore _store;

        public FlywheelParameterStoreGameComponent() : base()
        {
            _store = new FlywheelParameterStore();
        }

        public FlywheelParameterStoreGameComponent(global::Verse.Game game) : base()
        {
            _store = new FlywheelParameterStore();
        }

        public FlywheelParameterStore Store => _store;

        public override void ExposeData()
        {
            base.ExposeData();
            if (global::Verse.Scribe.mode == global::Verse.LoadSaveMode.Saving)
            {
                var (keys, values) = _store.GetSaveSnapshot();
                global::Verse.Scribe_Collections.Look(ref keys, "paramKeys");
                global::Verse.Scribe_Collections.Look(ref values, "paramValues");
            }
            else if (global::Verse.Scribe.mode == global::Verse.LoadSaveMode.LoadingVars)
            {
                var keys = new List<string>();
                var values = new List<float>();
                global::Verse.Scribe_Collections.Look(ref keys, "paramKeys");
                global::Verse.Scribe_Collections.Look(ref values, "paramValues");
                _store.LoadFromSnapshot(keys, values);
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            _store.FinalizeInit();
        }
    }
}
