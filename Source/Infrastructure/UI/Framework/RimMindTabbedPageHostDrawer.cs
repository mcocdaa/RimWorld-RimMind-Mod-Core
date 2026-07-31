using System.Collections.Generic;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.Framework
{
    public sealed class RimMindTabbedPageHostDrawer
    {
        public string DrawTabs(
            Rect root,
            IReadOnlyList<TabbedPageTabModel> tabs,
            string selectedId,
            RimMindLayoutScope scope)
        {
            var layout = TabbedPageLayout.Calculate(root, tabs);
            scope.Record(layout.TabBar, "TabbedPage:TabBar");
            scope.Record(layout.Content, "TabbedPage:Content");

            string nextSelected = selectedId;
            for (int i = 0; i < layout.TabRects.Count; i++)
            {
                var tabRect = layout.TabRects[i];
                var tab = tabs[i];
                scope.Record(tabRect.Rect, "TabbedPage:Tab:" + tab.Id);

                GUI.color = tab.Enabled ? Color.white : Color.gray;
                if (RimMindUI.DrawTabButton(tabRect.Rect, tab.Label, tabRect.Selected) && tab.Enabled)
                    nextSelected = tab.Id;
                GUI.color = Color.white;

                if (!string.IsNullOrEmpty(tab.TooltipKey))
                    TooltipHandler.TipRegion(tabRect.Rect, tab.TooltipKey.Translate());
            }

            return nextSelected;
        }
    }
}
