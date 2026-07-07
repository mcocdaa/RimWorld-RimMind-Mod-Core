using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI8_NoLegacyLayoutMathTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI8_NoLegacyTabOrSplitMathRemains()
    {
        var files = new[]
        {
            "Presentation/UI/AICoreSettingsUI.cs",
            "Infrastructure/UI/MainTabWindow_RimMindHub.cs",
            "Infrastructure/UI/DebugCenter/DebugCenterLayout.cs",
            "Infrastructure/UI/AgentsPage/AgentPageLayout.cs"
        };
        var forbidden = new[] { "CalcMaxPerRow", "TabMinWidth", "rect.width / _pages.Count", "Mathf.Clamp(rect.width * 0.28f" };
        var violations = new List<string>();

        foreach (string rel in files)
        {
            string text = File.ReadAllText(Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar)));
            foreach (string pattern in forbidden)
                if (text.Contains(pattern))
                    violations.Add(rel + " contains " + pattern);
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI8: remove legacy local layout math after framework migration.");
    }
}
