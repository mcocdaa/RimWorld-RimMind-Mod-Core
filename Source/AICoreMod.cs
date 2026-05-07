﻿﻿﻿using HarmonyLib;
using RimMind.Contracts.Extension;
using RimMind.Kernel.Context;
using RimMind.Core.Internal;
using RimMind.Core.Runtime;
using RimMind.Core.Settings;
using RimMind.Adapters.UI;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Json;
using UnityEngine;
using Verse;

namespace RimMind.Core
{
    public class RimMindCoreMod : Mod
    {
        public static RimMindCoreSettings Settings { get; private set; } = null!;

        public RimMindCoreMod(ModContentPack content) : base(content)
        {
            RimMindRuntime.Initialize();
            Settings = GetSettings<RimMindCoreSettings>();

            if (Settings.SavedModVersion != null && Settings.SavedModVersion != "2.0.0")
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    Verse.Log.Warning("[RimMind-Core] Saved mod version mismatch. Old saves may not be fully compatible with v2.0.");
                    Find.WindowStack.Add(new Verse.Dialog_MessageBox(
                        "RimMind.Core.UpgradeWarning".Translate(),
                        "OK".Translate(),
                        null));
                });
            }
            Settings.SavedModVersion = "2.0.0";

            JsonTagExtractor.OnWarning = Log.Warning;
            new Harmony("mcocdaa.RimMindCore").PatchAll();

            RimMindAPI.Extensions<IToggleBehavior>().Register(new CoreOverlayToggle(Settings));

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

    internal sealed class CoreOverlayToggle : IToggleBehavior
    {
        private readonly RimMindCoreSettings _settings;
        public CoreOverlayToggle(RimMindCoreSettings settings) { _settings = settings; }
        public string Id => "request_overlay";
        public bool IsActive => _settings.requestOverlayEnabled;
        public void Toggle()
        {
            _settings.requestOverlayEnabled = !_settings.requestOverlayEnabled;
            _settings.Write();
        }
    }
}
