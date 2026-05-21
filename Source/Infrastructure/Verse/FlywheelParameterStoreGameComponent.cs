using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class FlywheelParameterStoreGameComponent : GameComponent
    {
        private IFlywheelParameterStore? _store;

        public FlywheelParameterStoreGameComponent() : base() { }
        public FlywheelParameterStoreGameComponent(global::Verse.Game game) : base() { }

        // [Framework-Forced SL] Verse GameComponent requires parameterless constructor.
        // Lazy-cached SL.Get is the only viable pattern.
        public IFlywheelParameterStore Store => _store ??= RimMindServiceLocator.Get<IFlywheelParameterStore>()!;

        public override void ExposeData()
        {
            base.ExposeData();
            var store = Store;
            if (global::Verse.Scribe.mode == global::Verse.LoadSaveMode.Saving)
            {
                var (keys, values) = store.GetSaveSnapshot();
                global::Verse.Scribe_Collections.Look(ref keys, "paramKeys");
                global::Verse.Scribe_Collections.Look(ref values, "paramValues");
            }
            else if (global::Verse.Scribe.mode == global::Verse.LoadSaveMode.LoadingVars)
            {
                var keys = new List<string>();
                var values = new List<float>();
                global::Verse.Scribe_Collections.Look(ref keys, "paramKeys");
                global::Verse.Scribe_Collections.Look(ref values, "paramValues");
                store.LoadFromSnapshot(keys, values);
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Store.FinalizeInit();
        }
    }
}
