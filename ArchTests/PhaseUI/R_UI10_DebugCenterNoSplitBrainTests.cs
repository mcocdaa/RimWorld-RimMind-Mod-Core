using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI10_DebugCenterNoSplitBrainTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI10_AIRequests_Should_Not_Draw_Selection_Overlay_With_Second_ScrollView()
    {
        string path = Path.Combine(
            SourceDir,
            "Infrastructure",
            "UI",
            "DebugCenter",
            "Pages",
            "AIRequestsDebugCenterPageDrawer.cs");

        string text = File.ReadAllText(path);
        var violations = new List<string>();

        if (text.Contains("DrawSelectionOverlay", System.StringComparison.Ordinal))
            violations.Add("DrawSelectionOverlay");

        if (text.Contains("Widgets.ButtonInvisible(rowRect)", System.StringComparison.Ordinal))
            violations.Add("Widgets.ButtonInvisible(rowRect)");

        if (text.Contains("Widgets.ButtonInvisible(", System.StringComparison.Ordinal))
            violations.Add("Widgets.ButtonInvisible");

        if (text.Contains("TablePageLayout.Calculate", System.StringComparison.Ordinal))
            violations.Add("TablePageLayout.Calculate");

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI10: AI Requests row selection must be drawn by RimMindTableDrawer inside the table row scroll view.");
        text.Should().Contain("_tableDrawer.DrawSelectable",
            "CLEAN_UI_ERROR R-UI10: AI Requests must delegate selectable row rendering to RimMindTableDrawer.");
    }

    [Fact]
    public void R_UI10_DebugTablePages_Should_Not_Use_Empty_Row_Placeholders()
    {
        var files = new[]
        {
            "ToolCallsDebugCenterPageDrawer.cs",
            "MechanismsDebugCenterPageDrawer.cs",
            "ContextKeysDebugCenterPageDrawer.cs"
        };

        var violations = new List<string>();
        foreach (string file in files)
        {
            string path = Path.Combine(
                SourceDir,
                "Infrastructure",
                "UI",
                "DebugCenter",
                "Pages",
                file);

            string text = File.ReadAllText(path);
            if (text.Contains("Array.Empty<DebugTableRow>()", System.StringComparison.Ordinal))
                violations.Add(file);
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI10: debug table pages must show runtime rows from table model builders, not empty placeholders.");
    }

    [Fact]
    public void R_UI10_DebugTablePages_Should_Inherit_Shared_Table_Page_Base()
    {
        var files = new[]
        {
            "ToolCallsDebugCenterPageDrawer.cs",
            "MechanismsDebugCenterPageDrawer.cs",
            "ContextKeysDebugCenterPageDrawer.cs"
        };

        var violations = new List<string>();
        foreach (string file in files)
        {
            string path = Path.Combine(
                SourceDir,
                "Infrastructure",
                "UI",
                "DebugCenter",
                "Pages",
                file);

            string text = File.ReadAllText(path);
            if (!text.Contains(": DebugTablePageBase", System.StringComparison.Ordinal))
                violations.Add(file);
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI10: simple debug table pages must inherit DebugTablePageBase so shared table drawing stays centralized without hiding page dependencies.");
    }

    [Fact]
    public void R_UI10_Overview_Should_Navigate_Internal_Pages_Instead_Of_Opening_Old_Debug_Windows()
    {
        string path = Path.Combine(SourceDir, "Infrastructure", "UI", "DebugCenter", "Pages", "OverviewDebugCenterPageDrawer.cs");
        string text = File.ReadAllText(path);

        text.Should().NotContain("new Window_AgentFlowLab",
            "CLEAN_UI_ERROR R-UI10: overview quick actions must switch Debug Center pages instead of opening legacy windows.");
        text.Should().NotContain("new Window_AgentStateDebug",
            "CLEAN_UI_ERROR R-UI10: overview quick actions must switch Debug Center pages instead of opening legacy windows.");
        text.Should().NotContain("new Window_AgentModeDebug",
            "CLEAN_UI_ERROR R-UI10: overview quick actions must switch Debug Center pages instead of opening legacy windows.");
        text.Should().Contain("context.Navigation.GoTo",
            "CLEAN_UI_ERROR R-UI10: overview quick actions must use DebugCenterNavigation for internal page changes.");
    }
}
