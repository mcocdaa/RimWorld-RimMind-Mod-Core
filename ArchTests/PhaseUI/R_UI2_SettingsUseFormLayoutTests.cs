using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI2_SettingsUseFormLayoutTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI2_SettingsTabs_ShouldUseFormPageLayout()
    {
        var files = new[]
        {
            "Presentation/UI/ApiTabDrawer.cs",
            "Presentation/UI/QueueTabDrawer.cs",
            "Presentation/UI/ContextTabDrawer.cs",
            "Presentation/UI/PromptsTabDrawer.cs"
        };

        var violations = new List<string>();
        foreach (string rel in files)
        {
            string text = File.ReadAllText(Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar)));
            if (!text.Contains("FormPageLayout", StringComparison.Ordinal))
                violations.Add(rel);
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI2: Settings tabs must use FormPageLayout for predictable scroll height.");
    }
}
