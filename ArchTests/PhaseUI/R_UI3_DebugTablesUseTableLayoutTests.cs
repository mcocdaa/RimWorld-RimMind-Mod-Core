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
            "Infrastructure/UI/AIRequestsPage/AIRequestsPageDrawer.cs",
            "Infrastructure/UI/Window_RequestLog.cs",
            "Infrastructure/UI/Window_AIDebugLog.cs",
            "Infrastructure/UI/Window_ToolCallDebug.cs",
            "Infrastructure/UI/Window_MechanismStatus.cs",
            "Infrastructure/UI/Window_ContextKeyDebug.cs"
        };

        var violations = new List<string>();
        foreach (string rel in files)
        {
            string text = File.ReadAllText(Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar)));
            if (!text.Contains("TablePageLayout.Calculate", StringComparison.Ordinal))
                violations.Add(rel);
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI3: debug table/log pages must use TablePageLayout.");
    }
}
