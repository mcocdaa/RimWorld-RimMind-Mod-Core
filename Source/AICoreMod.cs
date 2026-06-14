using System;
using HarmonyLib;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Features.Json;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Context;
using RimMind.Presentation.Api;
using RimMind.Presentation.Context;
using RimMind.Presentation.UI;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Presentation
{
    public class RimMindCoreMod : Mod
    {
        public static RimMindCoreMod Instance { get; private set; } = null!;
        public static RimMindCoreSettings Settings { get; private set; } = null!;
        private static ISettingsProvider? _cachedSettingsProvider;

        public RimMindCoreMod(ModContentPack content) : base(content)
        {
            Instance = this;
            AssemblyLoadGuard.AssertAssembliesLoaded();

            RimMindServiceLocator.OnServiceNotFound = msg => Log.Warning(msg);

            Settings = GetSettings<RimMindCoreSettings>();
            var sp = new SettingsProvider(Settings);
            _cachedSettingsProvider = sp;

            RimMindRuntime.Initialize(sp, Settings);
            var runtime = RimMindRuntime.Instance;

            if (Settings.SavedModVersion != null && Settings.SavedModVersion != "2.0.0")
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    RimMindErrors.Warn("[RimMind-Core] Saved mod version mismatch. Old saves may not be fully compatible with v2.0.");
                    runtime.WindowService?.OpenUpgradeWarning();
                });
            }
            Settings.SavedModVersion = "2.0.0";

            JsonTagExtractor.OnWarning = msg => RimMindErrors.Warn(msg);
            new Harmony("mcocdaa.RimMindCore").PatchAll();

            RimMindAPI.Extensions<IToggleBehavior>().Register(new CoreOverlayToggle(sp));

            RimMindAPI.RegisterParameterTuner(new FlywheelBuiltinTuner());

            ScenarioRegistry.RegisterCoreScenarios(
                null,
                runtime.GetService<ILogSink>());

            // L3: Use instance-based RelevanceTable
            runtime.RelevanceTable.RegisterCoreRelevance();

            // L3: Register Core context providers via new ContextProviderDef API
            CoreContextProviders.RegisterAll(
                runtime.ContextKeys,
                runtime.GetService<ITranslationService>(),
                runtime.GetService<IContextKeyProvider>(),
                runtime.GetService<INpcManager>());
        }

        public override string SettingsCategory() => "RimMind";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RimMindCoreSettingsUI.Draw(inRect, _cachedSettingsProvider!);
        }
    }

    internal sealed class CoreOverlayToggle : IToggleBehavior
    {
        private readonly IOverlaySettings _settings;
        public CoreOverlayToggle(IOverlaySettings settings) { _settings = settings; }
        public string Id => "request_overlay";
        public string OwnerModId => "RimMindCore";
        public bool IsActive => _settings.RequestOverlayEnabled;
        public void Toggle()
        {
            _settings.RequestOverlayEnabled = !_settings.RequestOverlayEnabled;
            _settings.Persist();
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
