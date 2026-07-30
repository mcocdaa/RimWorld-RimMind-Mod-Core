using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Runtime.Services;
using RimMind.Presentation.UI.Layout;
using RimMind.Presentation.UI;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_RimMindSettings : RimMindWindowBase
    {
        private readonly RuntimeServiceRef<ISettingsProvider> _settingsProvider =
            RuntimeServiceRef<ISettingsProvider>.Required();

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public Window_RimMindSettings()
        {
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        protected override void DrawContents(Rect inRect, RimMindLayoutScope scope)
        {
            scope.Record(inRect, "Settings:Body");
            RimMindCoreSettingsUI.Draw(inRect, scope);
        }

        public override void PreClose()
        {
            _settingsProvider.Value.Persist();
            base.PreClose();
        }
    }
}
