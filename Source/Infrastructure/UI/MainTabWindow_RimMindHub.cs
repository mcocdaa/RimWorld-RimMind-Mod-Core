using System.Collections.Generic;
using System.Linq;
using RimMind.Infrastructure.UI.DebugCenter;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_RimMindHub : Window
    {
        private string _pageId;
        private readonly Pawn? _selectedPawn;
        private readonly DebugCenterPageContext _context;
        private readonly IReadOnlyList<IDebugCenterPageDrawer> _pages;

        public override Vector2 InitialSize => new Vector2(780f, 580f);

        public Window_RimMindHub()
            : this(DebugCenterPageRegistry.DefaultPageId, selectedPawn: null)
        {
        }

        private Window_RimMindHub(string initialPageId, Pawn? selectedPawn)
        {
            _pages = DebugCenterPageRegistry.CreateAll();
            _pageId = ResolvePageId(initialPageId);
            _selectedPawn = selectedPawn;
            _context = new DebugCenterPageContext(selectedPawn);
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        public static Window_RimMindHub OpenAgentsForPawn(Pawn selectedPawn)
            => new Window_RimMindHub("agents", selectedPawn);

        public static Window_RimMindHub OpenAIRequests()
            => new Window_RimMindHub("ai_requests", selectedPawn: null);

        public override void DoWindowContents(Rect inRect)
        {
            float y = RimMindUI.DrawWindowHeader(inRect, "RimMind.UI.Hub.Title".Translate());

            Rect tabRect = new Rect(inRect.x, y, inRect.width, RimMindUI.TabHeight);
            DrawTabs(tabRect);
            y = tabRect.yMax + RimMindUI.Padding;

            Rect contentRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            IDebugCenterPageDrawer? selectedPage = ResolveSelectedPage();
            selectedPage?.Draw(contentRect, _context);
        }

        private void DrawTabs(Rect rect)
        {
            if (_pages.Count == 0)
                return;

            float tabW = rect.width / _pages.Count;
            for (int i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];
                Rect tabBtn = new Rect(rect.x + i * tabW, rect.y, tabW - 2f, rect.height);
                bool selected = _pageId == page.Descriptor.Id;
                GUI.color = selected ? Color.white : Color.gray;
                if (Widgets.ButtonText(tabBtn, page.Descriptor.LabelKey.Translate()))
                    _pageId = page.Descriptor.Id;
            }

            GUI.color = Color.white;
        }

        private IDebugCenterPageDrawer? ResolveSelectedPage()
        {
            var selectedPage = _pages.FirstOrDefault(page => page.Descriptor.Id == _pageId)
                ?? _pages.FirstOrDefault(page => page.Descriptor.IsDefault)
                ?? _pages.FirstOrDefault();

            if (selectedPage != null)
                _pageId = selectedPage.Descriptor.Id;

            return selectedPage;
        }

        private static string ResolvePageId(string pageId)
        {
            return DebugCenterPageRegistry.Find(pageId)?.Id
                ?? DebugCenterPageRegistry.DefaultPageId;
        }
    }

    public class MainTabWindow_RimMindHub : Window_RimMindHub
    {
    }
}
