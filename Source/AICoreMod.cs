using System;
using HarmonyLib;
using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Features.Json;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Context;
using RimMind.Presentation.Api;
using RimMind.Presentation.UI;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Runtime.Services;
using RimMind.Presentation.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Presentation
{
    public class RimMindCoreMod : Mod
    {
        public static RimMindCoreMod Instance { get; private set; } = null!;
        public static RimMindCoreSettings Settings { get; private set; } = null!;

        public RimMindCoreMod(ModContentPack content) : base(content)
        {
            Instance = this;
            AssemblyLoadGuard.AssertAssembliesLoaded();

            Settings = GetSettings<RimMindCoreSettings>();
            var sp = new SettingsProvider(Settings);

            ProcessLifecycleEvents.Publisher.Configure(VerseLifecycleEventSink.Instance);
            RimMindRuntimeHost.Initialize(sp, Settings);
            var scope = RuntimeServiceHub.Shared.Capture();
            var runtime = scope.GetRequired<RimMindRuntime>();

            if (Settings.SavedModVersion != null && Settings.SavedModVersion != "2.0.0")
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    RimMindErrors.Warn("[RimMind-Core] Saved mod version mismatch. Old saves may not be fully compatible with v2.0.");
                    RuntimeServiceHub.Shared.Capture()
                        .GetOptional<RimMindRuntime>()?
                        .WindowService?
                        .OpenUpgradeWarning();
                });
            }
            Settings.SavedModVersion = "2.0.0";

            JsonTagExtractor.OnWarning = msg => RimMindErrors.Warn(msg);
            new Harmony("mcocdaa.RimMindCore").PatchAll();

            RimMindAPI.Extensions<IToggleBehavior>().Register(new CoreOverlayToggle());

            RimMindAPI.RegisterParameterTuner(new FlywheelBuiltinTuner());

            ScenarioRegistry.RegisterCoreScenarios(
                scope.GetRequired<ITranslationService>(),
                scope.GetRequired<ILogSink>());

            // L3: Use instance-based RelevanceTable
            runtime.RelevanceTable.RegisterCoreRelevance();

        }

        public override string SettingsCategory() => "RimMind";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RimMindCoreSettingsUI.Draw(inRect);
        }
    }

    internal sealed class CoreOverlayToggle : IToggleBehavior
    {
        private readonly RuntimeServiceRef<ISettingsProvider> _settings =
            RuntimeServiceRef<ISettingsProvider>.Optional();

        public string Id => "request_overlay";
        public string OwnerModId => RimMindOwnerConsts.CoreModId;
        public bool IsActive => _settings.ValueOrDefault?.RequestOverlayEnabled == true;

        public void Toggle()
        {
            var settings = _settings.ValueOrDefault;
            if (settings == null)
            {
                RimMindErrors.Warn("[RimMind-Core] Request overlay settings are unavailable while the runtime is stopped.");
                return;
            }

            settings.RequestOverlayEnabled = !settings.RequestOverlayEnabled;
            settings.Persist();
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
