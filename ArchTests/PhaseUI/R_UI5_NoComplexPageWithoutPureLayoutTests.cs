using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI5_NoComplexPageWithoutPureLayoutTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    private static readonly HashSet<string> DeferredLegacyWindows = new(StringComparer.Ordinal)
    {
        // Deferred to the next UI migration batch: these windows are outside the debug-center/settings scope of the
        // current refactor but should move to split/table/form layouts before the compatibility cleanup is complete.
        "Infrastructure/UI/RequestOverlay.cs",
        "Infrastructure/UI/Window_AgentDialogue.cs",
        "Infrastructure/UI/Window_AgentFlowLab.cs",
        "Infrastructure/UI/Window_AgentModeDebug.cs",
        "Infrastructure/UI/Window_AgentProgressFloat.cs",
        "Infrastructure/UI/Window_AgentStateDebug.cs",

        // Shared drawing helper and Agent child drawers are controlled by AgentPageLayout in the parent page.
        "Infrastructure/UI/RimMindUI.cs",
        "Infrastructure/UI/AgentsPage/AgentActivityStreamDrawer.cs",
        "Infrastructure/UI/AgentsPage/AgentListPanelDrawer.cs"
    };

    [Fact]
    public void R_UI5_ComplexDebugPages_ShouldReferenceSharedLayoutModels()
    {
        string uiDir = Path.Combine(SourceDir, "Infrastructure", "UI");
        var files = Directory.GetFiles(uiDir, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (string file in files)
        {
            string rel = file.Substring(SourceDir.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
            string text = File.ReadAllText(file);
            bool isComplexPage = text.Contains("BeginScrollView", StringComparison.Ordinal)
                || text.Contains("DrawList", StringComparison.Ordinal)
                || text.Contains("DrawDetail", StringComparison.Ordinal)
                || text.Contains("ButtonText(new Rect", StringComparison.Ordinal);
            bool usesFramework = text.Contains("TabbedPageLayout", StringComparison.Ordinal)
                || text.Contains("SplitPageLayout", StringComparison.Ordinal)
                || text.Contains("FormPageLayout", StringComparison.Ordinal)
                || text.Contains("TablePageLayout", StringComparison.Ordinal)
                || text.Contains("ActionBarLayout", StringComparison.Ordinal);

            if (isComplexPage
                && !usesFramework
                && !rel.Contains("/Framework/", StringComparison.Ordinal)
                && !DeferredLegacyWindows.Contains(rel))
            {
                violations.Add(rel);
            }
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI5: complex UI pages must use shared layout models.");
    }
}
