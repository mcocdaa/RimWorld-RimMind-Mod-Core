using System;
using RimMind.Application.Common.Interfaces.Internal;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    [Obsolete("Use RimMind.Presentation.UI.RimMindCoreSettingsUI instead. This forwarding class will be removed in a future version.")]
    public static class RimMindCoreSettingsUI
    {
        public static void Draw(Rect inRect, ISettingsProvider settings)
            => RimMind.Presentation.UI.RimMindCoreSettingsUI.Draw(inRect, settings);
    }
}
