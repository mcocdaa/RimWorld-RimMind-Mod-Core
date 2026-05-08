using UnityEngine;
using Verse;

namespace RimMind.Adapters.UI
{
    public static class SettingsUIHelper
    {
        public static void DrawSectionHeader(Listing_Standard listing, string label)
        {
            listing.Gap(12f);
            GUI.color = new Color(0.8f, 0.85f, 1f);
            listing.Label(label);
            GUI.color = Color.white;
            listing.Gap(4f);
        }

        public static void DrawCustomPromptSection(Listing_Standard listing, string label, ref string value, float height)
        {
            listing.Label(label);
            value = listing.TextEntry(value, (int)height);
            listing.Gap(4f);
        }
    }
}
