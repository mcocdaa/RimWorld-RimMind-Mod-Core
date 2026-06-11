using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI;
using RimMind.Application.Api;
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

            // Sync icon visual state from toggle registry before rendering.
            // ToggleableIcon flips _iconState via ref on click, so we compare
            // prev vs post to detect user interaction, then route by modifier.
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
                Find.WindowStack.Add(new Window_RimMindHub());
            }
            else
            {
                ToggleCoreOverlay();
            }

            // Reset visual state to match actual toggle state (not the widget flip)
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

        private static void ToggleCoreOverlay()
        {
            var registry = RimMindAPI.Extensions<IToggleBehavior>();
            if (registry == null) return;
            foreach (var toggle in registry.All)
            {
                if (toggle.Id == "request_overlay")
                {
                    toggle.Toggle();
                    return;
                }
            }
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
