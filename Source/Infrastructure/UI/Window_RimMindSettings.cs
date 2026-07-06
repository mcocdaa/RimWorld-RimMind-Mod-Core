using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI.Layout;
using RimMind.Presentation.UI;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_RimMindSettings : RimMindWindowBase
    {
        private readonly ISettingsProvider _settingsProvider;

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public Window_RimMindSettings(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        protected override void DrawContents(Rect inRect, RimMindLayoutScope scope)
        {
            RimMindCoreSettingsUI.Draw(inRect, _settingsProvider, scope);
        }

        public override void PreClose()
        {
            _settingsProvider.Persist();
            base.PreClose();
        }
    }
}
