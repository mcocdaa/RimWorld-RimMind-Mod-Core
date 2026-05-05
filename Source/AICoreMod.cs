using HarmonyLib;
using RimMind.Core.Context;
using RimMind.Core.Flywheel;
using RimMind.Core.Internal;
using RimMind.Core.Settings;
using RimMind.Core.UI;
using UnityEngine;
using Verse;

namespace RimMind.Core
{
    public class RimMindCoreMod : Mod
    {
        public static RimMindCoreSettings Settings { get; private set; } = null!;

        public RimMindCoreMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMindCoreSettings>();
            JsonTagExtractor.OnWarning = Log.Warning;
            new Harmony("mcocdaa.RimMindCore").PatchAll();

            RimMindAPI.RegisterToggleBehavior("request_overlay",
                () => Settings.requestOverlayEnabled,
                () =>
                {
                    Settings.requestOverlayEnabled = !Settings.requestOverlayEnabled;
                    Settings.Write();
                });

            RimMindAPI.RegisterParameterTuner(new FlywheelBuiltinTuner());

            ScenarioRegistry.RegisterCoreScenarios();
            RelevanceTable.RegisterCoreRelevance();
            ContextKeyRegistry.RegisterCoreKeys();
        }

        public override string SettingsCategory() => "RimMind";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RimMindCoreSettingsUI.Draw(inRect);
        }
    }
}
