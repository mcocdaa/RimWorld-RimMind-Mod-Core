using System;
using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Features.Flywheel;
using RimMind.Presentation.Context;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
using RimMind.Infrastructure.UI;
using UnityEngine;
using Verse;

namespace RimMind.Presentation
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
                    Find.WindowStack.Add(new Verse.Dialog_MessageBox(
                        "RimMind.Core.UpgradeWarning".Translate(),
                        "OK".Translate(),
                        null));
                });
            }
            Settings.SavedModVersion = "2.0.0";

            new Harmony("mcocdaa.RimMindCore").PatchAll();

            ContextKeyRegistry.RegisterCoreKeys();
        }

        public override string SettingsCategory() => "RimMind";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RimMindCoreSettingsUI.Draw(inRect);
        }
    }
}
