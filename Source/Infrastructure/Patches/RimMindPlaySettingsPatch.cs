using System.Collections.Generic;
using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI;
using RimMind.Presentation;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.Patches
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    [StaticConstructorOnStartup]
    public static class RimMindPlaySettingsPatch
    {
        private static readonly Texture2D Icon =
            ContentFinder<Texture2D>.Get("UI/RimMind/Icon", reportFailure: false) ?? BaseContent.BadTex;

        private static bool _iconState;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null) return;

            _iconState = IsAnyToggleActive();

            bool prev = _iconState;
            row.ToggleableIcon(
                ref _iconState,
                Icon,
                "RimMind.Presentation.Toggle.Tooltip".Translate(),
                SoundDefOf.Mouseover_ButtonToggle);

            if (_iconState == prev) return;

            bool shift = Event.current.shift;
            bool control = Event.current.control;

            if (shift)
            {
                OpenSettings();
            }
            else if (control)
            {
                Find.WindowStack.Add(new Window_RequestLog());
            }
            else
            {
                OpenCoreMenu();
            }

            _iconState = IsAnyToggleActive();
        }

        private static bool IsAnyToggleActive()
        {
            var registry = RimMindAPI.Extensions<IToggleBehavior>();
            if (registry == null) return false;
            foreach (var toggle in registry.All)
            {
                if (toggle.IsActive) return true;
            }
            return false;
        }

        private static void OpenCoreMenu()
        {
            Pawn? selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("RimMind.UI.OverlayMenu.RequestLog".Translate(), () =>
                    Find.WindowStack.Add(new Window_RequestLog())),
                new FloatMenuOption("RimMind.UI.OverlayMenu.ToolCallDebug".Translate(), () =>
                    Find.WindowStack.Add(new Window_ToolCallDebug())),
                new FloatMenuOption("RimMind.UI.OverlayMenu.MechanismStatus".Translate(), () =>
                    Find.WindowStack.Add(new Window_MechanismStatus())),
                new FloatMenuOption("RimMind.UI.OverlayMenu.AgentModeDebug".Translate(), () =>
                    Find.WindowStack.Add(new Window_AgentModeDebug(selectedPawn))),
                new FloatMenuOption("RimMind.UI.OverlayMenu.AgentState".Translate(), () =>
                    Find.WindowStack.Add(new Window_AgentStateDebug(selectedPawn))),
                new FloatMenuOption("RimMind.UI.OverlayMenu.ContextKeys".Translate(), () =>
                    Find.WindowStack.Add(new Window_ContextKeyDebug())),
                new FloatMenuOption("RimMind.UI.OverlayMenu.AgentFlowLab".Translate(), () =>
                    Find.WindowStack.Add(new Window_AgentFlowLab(selectedPawn))),
                new FloatMenuOption("RimMind.UI.OverlayMenu.AgentProgress".Translate(), () =>
                    Find.WindowStack.Add(new Window_AgentProgressFloat())),
                new FloatMenuOption("RimMind.UI.OverlayMenu.Settings".Translate(), OpenSettings)
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void OpenSettings()
        {
            var sp = RimMindServiceLocator.Get<ISettingsProvider>();
            if (sp != null)
            {
                Find.WindowStack.Add(new Window_RimMindSettings(sp));
            }
        }
    }
}
