using System.Collections.Generic;
using System.Linq;
using RimMind.Infrastructure.UI.DebugCenter;
using RimMind.Infrastructure.UI.Framework;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_RimMindHub : RimMindWindowBase
    {
        private string _pageId;
        private readonly Pawn? _selectedPawn;
        private readonly DebugCenterPageContext _context;
        private readonly IReadOnlyList<IDebugCenterPageDrawer> _pages;
        private readonly RimMindTabbedPageHostDrawer _tabDrawer = new();

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

        protected override void DrawContents(Rect inRect, RimMindLayoutScope scope)
        {
            Rect body = inRect.InsetSafe(RimMindUiMetrics.WindowInset);
            Rect header = new Rect(body.x, body.y, body.width, RimMindUiMetrics.HeaderHeight);
            Rect tabRoot = new Rect(
                body.x,
                header.yMax + RimMindUiMetrics.Padding,
                body.width,
                Mathf.Max(1f, body.yMax - header.yMax - RimMindUiMetrics.Padding));
            var tabs = BuildTabModels();
            var tabLayout = TabbedPageLayout.Calculate(tabRoot, tabs);

            scope.Record(header, "Hub:Header");
            RimMindUI.DrawWindowHeader(header, "RimMind.UI.Hub.Title".Translate());
            _pageId = _tabDrawer.DrawTabs(tabRoot, tabs, _pageId, scope);
            IDebugCenterPageDrawer? selectedPage = ResolveSelectedPage();
            selectedPage?.Draw(tabLayout.Content, _context, scope);
        }

        private IReadOnlyList<TabbedPageTabModel> BuildTabModels()
            => _pages
                .Select(page => new TabbedPageTabModel(
                    page.Descriptor.Id,
                    page.Descriptor.LabelKey.Translate(),
                    page.Descriptor.LabelKey,
                    _pageId == page.Descriptor.Id,
                    enabled: true,
                    tooltipKey: null))
                .ToList();

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
