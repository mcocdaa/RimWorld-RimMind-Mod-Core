using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    /// <summary>
    /// Base class for all RimMind Window subclasses. Seals DoWindowContents and
    /// delegates to DrawContents, wrapping the call in a RimMindLayoutScope so
    /// every frame's layout conflicts are auto-published to LayoutConflictStore.
    /// Concrete windows MUST override DrawContents and use the scope for every
    /// rect they draw.
    /// </summary>
    public abstract class RimMindWindowBase : Window
    {
        public override sealed void DoWindowContents(Rect inRect)
        {
            // RimWorld draws a snapshot of WindowStack. A window closed earlier in the
            // same OnGUI pass can therefore receive one final draw after PreClose.
            if (!IsOpen)
                return;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            using (var scope = RimMindLayoutScope.Begin(GetType().Name, inRect))
            {
                DrawContents(inRect, scope);
            }
        }

        /// <summary>
        /// Draw the window body. Every drawn rect SHOULD be registered with the
        /// scope (either via scope.Record or by passing the recorder through
        /// RimMindUI overloads) so conflicts are detected.
        /// </summary>
        protected abstract void DrawContents(Rect inRect, RimMindLayoutScope scope);
    }
}
