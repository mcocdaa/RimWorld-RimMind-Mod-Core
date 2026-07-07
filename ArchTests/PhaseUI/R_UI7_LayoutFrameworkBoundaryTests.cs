using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI7_LayoutFrameworkBoundaryTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI7_PresentationFramework_ShouldNotDependOnInfrastructure()
    {
        string dir = Path.Combine(SourceDir, "Presentation", "UI", "Framework");
        var violations = new List<string>();

        foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            string rel = file.Substring(SourceDir.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
            if (text.Contains("RimMind.Infrastructure"))
                violations.Add(rel);
            if (text.Contains("Verse."))
                violations.Add(rel + " uses Verse");
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI7: pure UI framework must not depend on Infrastructure or Verse.");
    }
}
