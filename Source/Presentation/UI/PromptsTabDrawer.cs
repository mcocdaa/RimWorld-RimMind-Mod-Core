using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Internal;
using UnityEngine;
using Verse;

namespace RimMind.Presentation.UI
{
    internal static class PromptsTabDrawer
    {
        private static Vector2 _promptsScroll;

        public static void Draw(Rect inRect, ISettingsProvider s)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 460f);
            Widgets.BeginScrollView(inRect, ref _promptsScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            GUI.color = Color.gray;
            listing.Label("RimMind.Prompts.Desc".Translate());
            GUI.color = Color.white;
            listing.Gap(8f);

            var customPawnPrompt = s.CustomPawnPrompt;
            SettingsUIDrawer.DrawCustomPromptSection(listing,
                "RimMind.Prompts.PawnPromptLabel".Translate(),
                ref customPawnPrompt, 100f);
            s.CustomPawnPrompt = customPawnPrompt;

            listing.Gap(12f);

            var customMapPrompt = s.CustomMapPrompt;
            SettingsUIDrawer.DrawCustomPromptSection(listing,
                "RimMind.Prompts.MapPromptLabel".Translate(),
                ref customMapPrompt, 100f);
            s.CustomMapPrompt = customMapPrompt;

            listing.End();
            Widgets.EndScrollView();
        }
    }
}
