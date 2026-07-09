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

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI10: AI Requests row selection must be drawn by RimMindTableDrawer inside the table row scroll view.");
    }
}
