using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI3_DebugTablesUseTableLayoutTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI3_DebugTablePages_ShouldUseTablePageLayout()
    {
        var files = new[]
        {
            "Infrastructure/UI/DebugCenter/Pages/MechanismsDebugCenterPageDrawer.cs",
            "Infrastructure/UI/DebugCenter/Pages/ContextKeysDebugCenterPageDrawer.cs"
        };

        var violations = new List<string>();
        foreach (string rel in files)
        {
            string text = ReadSource(rel);
            if (!text.Contains("TablePageLayout", StringComparison.Ordinal)
                || !text.Contains("DebugTableModel", StringComparison.Ordinal))
                violations.Add(rel);
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI3: debug center table pages must use TablePageLayout and DebugTableModel.");
    }

    [Fact]
    public void R_UI3_ToolCalls_ShouldUseSharedDebugTableDrawer()
    {
        string text = ReadSource("Infrastructure/UI/DebugCenter/Pages/ToolCallsDebugCenterPageDrawer.cs");

        text.Should().Contain("DebugTableModel", "CLEAN_UI_ERROR R-UI3: ToolCalls must still build a DebugTableModel.");
        text.Should().Contain("_tableDrawer.Draw", "CLEAN_UI_ERROR R-UI3: ToolCalls rendering must be delegated to RimMindTableDrawer.");
        text.Should().NotContain("TablePageLayout.Calculate", "CLEAN_UI_ERROR R-UI3: migrated debug table pages must not keep discarded TablePageLayout calls.");
    }

    [Fact]
    public void R_UI3_AIRequests_ShouldUseSelectableDebugTableDrawer()
    {
        string text = ReadSource("Infrastructure/UI/DebugCenter/Pages/AIRequestsDebugCenterPageDrawer.cs");

        text.Should().Contain("DebugTableModel", "CLEAN_UI_ERROR R-UI3: AI Requests must still build a DebugTableModel.");
        text.Should().Contain("_tableDrawer.DrawSelectable", "CLEAN_UI_ERROR R-UI3: AI Requests selection must be delegated to RimMindTableDrawer.DrawSelectable.");
    }

    [Fact]
    public void R_UI3_RimMindTableDrawer_ShouldOwnSelectableTableLayout()
    {
        string text = ReadSource("Infrastructure/UI/Framework/RimMindTableDrawer.cs");

        text.Should().Contain("DrawSelectable", "CLEAN_UI_ERROR R-UI3: selectable debug tables must be exposed by RimMindTableDrawer.");
        text.Should().Contain("TablePageLayout.Calculate", "CLEAN_UI_ERROR R-UI3: RimMindTableDrawer must own TablePageLayout calculation for debug tables.");
    }

    private static string ReadSource(string relativePath)
    {
        return File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
