using System;
using HarmonyLib;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Settings;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Core.Runtime;
using RimMind.Kernel.Logging;
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
            AssemblyLoadGuard.AssertAssembliesLoaded();

            RimMindRuntime.Initialize();
            Settings = GetSettings<RimMindCoreSettings>();

            if (Settings.SavedModVersion != null && Settings.SavedModVersion != "2.0.0")
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    global::Verse.Log.Warning("[RimMind-Core] Saved mod version mismatch. Old saves may not be fully compatible with v2.0.");
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

    internal static class AssemblyLoadGuard
    {
        public static void AssertAssembliesLoaded()
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            var contracts = System.Linq.Enumerable.FirstOrDefault(loaded, a => a.GetName().Name == "0_RimMindContracts");
            var kernel = System.Linq.Enumerable.FirstOrDefault(loaded, a => a.GetName().Name == "1_RimMindKernel");

            if (contracts == null)
            {
                var msg = "[RimMind-Core] FATAL: 0_RimMindContracts.dll not loaded. " +
                          "Check that the dll exists in Assemblies/ folder. " +
                          "If you upgraded from v1.x, please subscribe to the new mod files.";
                Log.Error(msg);
                throw new System.InvalidOperationException(msg);
            }

            if (kernel == null)
            {
                var msg = "[RimMind-Core] FATAL: 1_RimMindKernel.dll not loaded. " +
                          "Check that the dll exists in Assemblies/ folder.";
                Log.Error(msg);
                throw new System.InvalidOperationException(msg);
            }

            Log.Message($"[RimMind-Core] Assemblies loaded: " +
                        $"Contracts={contracts.GetName().Version} " +
                        $"Kernel={kernel.GetName().Version} " +
                        $"Core={typeof(RimMindCoreMod).Assembly.GetName().Version}");
        }
    }
}
