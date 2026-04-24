using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimMind.Core.Flywheel
{
    public class FlywheelGameComponent : GameComponent
    {
        public FlywheelGameComponent() : base() { }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
                RimMindAPI.Telemetry.Flush();
        }
    }

    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    public static class FlywheelGameComponent_Register
    {
        static void Postfix(Game __instance)
        {
            if (!__instance.components.Any(c => c is FlywheelGameComponent))
                __instance.components.Add(new FlywheelGameComponent());
        }
    }
}
