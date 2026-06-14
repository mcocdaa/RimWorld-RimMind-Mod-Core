using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseM;

public sealed class R_M3_ApplicationStaticStateBoundaryTests
{
    [Fact]
    [Trait("Phase", "M")]
    public void R_M3_Application_Should_Not_Expose_Global_Instance_Singletons()
    {
        string sourceDir = ArchTestExtensions.FindSourceDirectory();
        string appDir = Path.Combine(sourceDir, "Application");
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "NullAgentActionBridge.cs",
            "NullDialogueTrigger.cs",
            "NullIncidentExecutedListener.cs",
            "NullModCooldown.cs",
            "NullSkipCheck.cs"
        };

        var violations = Directory.GetFiles(appDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !allowed.Contains(Path.GetFileName(f)))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"public\s+static\s+.*\bInstance\b"))
            .Select(f => f.Substring(sourceDir.Length + 1))
            .ToList();

        violations.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M3-STATIC-STATE: Application services must be provided through composition, not global Instance singletons. Violating files:\n{0}\nFix: inject services through constructors or register them from RimMindCompositionRoot.",
            string.Join("\n", violations));
    }
}
