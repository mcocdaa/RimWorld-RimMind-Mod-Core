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
            "Infrastructure/UI/DebugCenter/Pages/AIRequestsDebugCenterPageDrawer.cs",
            "Infrastructure/UI/DebugCenter/Pages/ToolCallsDebugCenterPageDrawer.cs",
            "Infrastructure/UI/DebugCenter/Pages/MechanismsDebugCenterPageDrawer.cs",
            "Infrastructure/UI/DebugCenter/Pages/ContextKeysDebugCenterPageDrawer.cs"
        };

        var violations = new List<string>();
        foreach (string rel in files)
        {
            string text = File.ReadAllText(Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar)));
            if (!text.Contains("TablePageLayout", StringComparison.Ordinal)
                || !text.Contains("DebugTableModel", StringComparison.Ordinal))
                violations.Add(rel);
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI3: debug center table pages must use TablePageLayout and DebugTableModel.");
    }
}
