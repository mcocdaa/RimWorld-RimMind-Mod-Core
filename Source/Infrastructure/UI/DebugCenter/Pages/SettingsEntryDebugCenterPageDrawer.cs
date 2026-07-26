using System;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Runtime.Services;
using RimMind.Presentation.UI.Layout;
using RimMind.Infrastructure.Verse;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class SettingsEntryDebugCenterPageDrawer : IRuntimeBoundDebugCenterPageDrawer
    {
        private ISettingsProvider? _settingsProvider;

        public IDisposable? Bind(RuntimeServiceScope scope)
        {
            _settingsProvider = scope.GetOptional<ISettingsProvider>();
            return null;
        }

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

        private void OpenSettings()
        {
            if (_settingsProvider != null)
            {
                Find.WindowStack.Add(new Window_RimMindSettings(_settingsProvider));
            }
        }
    }
}
