using System;
using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Context;
using RimMind.Presentation.Settings;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Context;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Presentation.Runtime;
using RimMind.Application.Features.Json;
using RimMind.Application.Features.Flywheel;
using UnityEngine;
using Verse;

namespace RimMind.Presentation
{
    public class RimMindCoreMod : Mod
    {
        public static RimMindCoreSettings Settings { get; private set; } = null!;

        public RimMindCoreMod(ModContentPack content) : base(content)
        {
            AssemblyLoadGuard.AssertAssembliesLoaded();

            RimMindRuntime.Initialize();
            Settings = GetSettings<RimMindCoreSettings>();
            RimMindServiceLocator.Register<ISettingsProvider>(new SettingsProvider(Settings));

            if (Settings.SavedModVersion != null && Settings.SavedModVersion != "2.0.0")
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    RimMindErrors.Warn("[RimMind-Core] Saved mod version mismatch. Old saves may not be fully compatible with v2.0.");
                    Find.WindowStack.Add(new Verse.Dialog_MessageBox(
                        "RimMind.Core.UpgradeWarning".Translate(),
                        "OK".Translate(),
                        null));
                });
            }
            Settings.SavedModVersion = "2.0.0";

            JsonTagExtractor.OnWarning = msg => RimMindErrors.Warn(msg);
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
            var domain = System.Linq.Enumerable.FirstOrDefault(loaded, a => a.GetName().Name == "0_RimMindDomain");
            var application = System.Linq.Enumerable.FirstOrDefault(loaded, a => a.GetName().Name == "1_RimMindApplication");

            if (domain == null)
            {
                var msg = "[RimMind-Core] FATAL: 0_RimMindDomain.dll not loaded. " +
                          "Check that the dll exists in Assemblies/ folder. " +
                          "If you upgraded from v1.x, please subscribe to the new mod files.";
                RimMindErrors.Error(msg);
                throw new System.InvalidOperationException(msg);
            }

            if (application == null)
            {
                var msg = "[RimMind-Core] FATAL: 1_RimMindApplication.dll not loaded. " +
                          "Check that the dll exists in Assemblies/ folder.";
                RimMindErrors.Error(msg);
                throw new System.InvalidOperationException(msg);
            }

            Log.Message($"[RimMind-Core] Assemblies loaded: " +
                        $"Domain={domain.GetName().Version} " +
                        $"Application={application.GetName().Version} " +
                        $"Core={typeof(RimMindCoreMod).Assembly.GetName().Version}");
        }
    }
}
