using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI1_NoAdHocTabLayoutTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI1_SettingsAndDebugCenter_ShouldUseSharedTabbedPageLayout()
    {
        var violations = new List<string>();
        string[] relativePaths =
        {
            Path.Combine("Presentation", "UI", "AICoreSettingsUI.cs"),
            Path.Combine("Infrastructure", "UI", "MainTabWindow_RimMindHub.cs")
        };

        foreach (string relativePath in relativePaths)
        {
            string file = Path.Combine(SourceDir, relativePath);
            string text = File.ReadAllText(file);
            string displayPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

            if (!text.Contains("TabbedPageLayout.Calculate", StringComparison.Ordinal))
                violations.Add(displayPath + " does not call TabbedPageLayout.Calculate");
            if (text.Contains("CalcMaxPerRow", StringComparison.Ordinal)
                || text.Contains("rect.width / _pages.Count", StringComparison.Ordinal))
                violations.Add(displayPath + " uses ad hoc tab math");
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI1: Settings and Debug Center must share TabbedPageLayout.");
    }
}
