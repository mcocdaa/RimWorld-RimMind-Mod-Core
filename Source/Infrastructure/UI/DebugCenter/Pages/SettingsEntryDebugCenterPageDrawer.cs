using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.UI.Layout;
using RimMind.Infrastructure.Verse;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class SettingsEntryDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "settings",
            "RimMind.UI.Hub.Tab.Settings",
            60,
            IsDefault: false);

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            scope.Record(rect, "Hub:SettingsEntry");

            float y = rect.y;
            y = RimMindUI.DrawSectionHeader(rect, y - rect.y, "RimMind.UI.Hub.SettingsEntryTitle".Translate()) + rect.y;
            y = RimMindUI.DrawWrappedLabel(
                rect,
                y - rect.y,
                "RimMind.UI.Hub.SettingsEntryDescription".Translate(),
                RimMindUI.ColorValue,
                scope.Recorder) + rect.y;

            y += RimMindUI.SectionGap;
            Rect buttonRect = new Rect(rect.x + RimMindUI.Padding, y, 180f, RimMindUI.BtnHeight);
            scope.Record(buttonRect, "Hub:SettingsEntry:OpenSettings");
            if (Widgets.ButtonText(buttonRect, "RimMind.UI.Hub.OpenSettings".Translate()))
            {
                OpenSettings();
            }
        }

        private static void OpenSettings()
        {
            var settingsProvider = RimMindServiceLocator.Get<ISettingsProvider>();
            if (settingsProvider != null)
            {
                Find.WindowStack.Add(new Window_RimMindSettings(settingsProvider));
            }
        }
    }
}
