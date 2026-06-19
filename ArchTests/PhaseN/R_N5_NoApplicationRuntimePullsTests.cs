using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN;

public sealed class R_N5_NoApplicationRuntimePullsTests
{
    [Fact]
    [Trait("Phase", "N")]
    public void R_N5_Application_Should_Not_Pull_Runtime_Or_ServiceLocator()
    {
        string sourceDir = ArchTestExtensions.FindSourceDirectory();
        string appDir = Path.Combine(sourceDir, "Application");

        var forbidden = new[]
        {
            @"RimMindRuntime\.Instance",
            @"RimMindServiceLocator\.(Get|TryGet|Register|Reset)",
            @"Find\.WindowStack",
            @"Verse\.Find",
            @"UnityEngine"
        };

        var violations = new List<string>();
        foreach (string file in Directory.GetFiles(appDir, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (string pattern in forbidden)
            {
                if (Regex.IsMatch(text, pattern))
                {
                    violations.Add($"{file.Substring(sourceDir.Length + 1)} violates {pattern}");
                }
            }
        }

        violations.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-N5: Application must receive dependencies through contracts and composition, not runtime/service-locator pulls.\n{0}",
            string.Join("\n", violations));
    }
}
