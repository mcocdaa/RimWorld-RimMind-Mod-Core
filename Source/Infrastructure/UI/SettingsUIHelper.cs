using System;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    [Obsolete("Use RimMind.Presentation.UI.SettingsUIHelper instead. This forwarding class will be removed in a future version.")]
    public static class SettingsUIHelper
    {
        public static void DrawSectionHeader(Listing_Standard listing, string label)
            => RimMind.Presentation.UI.SettingsUIHelper.DrawSectionHeader(listing, label);

        public static void DrawCustomPromptSection(Listing_Standard listing, string label, ref string value, float height)
            => RimMind.Presentation.UI.SettingsUIHelper.DrawCustomPromptSection(listing, label, ref value, height);

        public static Rect SplitContentArea(Rect inRect)
            => RimMind.Presentation.UI.SettingsUIHelper.SplitContentArea(inRect);

        public static Rect SplitBottomBar(Rect inRect)
            => RimMind.Presentation.UI.SettingsUIHelper.SplitBottomBar(inRect);

        public static void DrawBottomBar(Rect bottomBar, Action resetAction)
            => RimMind.Presentation.UI.SettingsUIHelper.DrawBottomBar(bottomBar, resetAction);
    }
}
