using System.Collections.Generic;
using RimMind.Kernel.Flywheel;
using Verse;

namespace RimMind.Adapters.Verse
{
    public class FlywheelParameterStoreGameComponent : GameComponent
    {
        private readonly FlywheelParameterStore _store;

        public FlywheelParameterStoreGameComponent() : base()
        {
            _store = new FlywheelParameterStore();
        }

        public FlywheelParameterStoreGameComponent(Verse.Game game) : base()
        {
            _store = new FlywheelParameterStore();
        }

        public FlywheelParameterStore Store => _store;

        public override void ExposeData()
        {
            base.ExposeData();
            if (Verse.Scribe.mode == Verse.LoadSaveMode.Saving)
            {
                var (keys, values) = _store.GetSaveSnapshot();
                Verse.Scribe_Collections.Look(ref keys, "paramKeys");
                Verse.Scribe_Collections.Look(ref values, "paramValues");
            }
            else if (Verse.Scribe.mode == Verse.LoadSaveMode.LoadingVars)
            {
                var keys = new List<string>();
                var values = new List<float>();
                Verse.Scribe_Collections.Look(ref keys, "paramKeys");
                Verse.Scribe_Collections.Look(ref values, "paramValues");
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
