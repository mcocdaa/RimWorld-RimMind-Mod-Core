using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseM;

public sealed class R_M2_NamespaceCleanArchitectureBoundaryTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    private static IEnumerable<string> CsFiles(string relativeDir)
    {
        string dir = Path.Combine(SourceDir, relativeDir);
        return Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}backup{Path.DirectorySeparatorChar}"))
            : Enumerable.Empty<string>();
    }

    private static List<string> FindViolations(string relativeDir, params string[] forbiddenPatterns)
    {
        var violations = new List<string>();
        foreach (string file in CsFiles(relativeDir))
        {
            string text = File.ReadAllText(file);
            foreach (string pattern in forbiddenPatterns)
            {
                if (Regex.IsMatch(text, pattern, RegexOptions.Multiline))
                {
                    string rel = file.Substring(SourceDir.Length + 1);
                    violations.Add($"{rel} violates {pattern}");
                    break;
                }
            }
        }

        return violations;
    }

    [Fact]
    [Trait("Phase", "M")]
    public void R_M2_Domain_Should_Not_Know_Outer_Layers_Or_Frameworks()
    {
        var violations = FindViolations("Domain",
            @"using\s+RimMind\.Application",
            @"using\s+RimMind\.Presentation",
            @"using\s+RimMind\.Infrastructure",
            @"using\s+Verse",
            @"using\s+UnityEngine",
            @"using\s+HarmonyLib",
            @"using\s+RimWorld");

        violations.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M2-NAMESPACE: Domain must not know outer layers or game frameworks. Violations:\n{0}",
            string.Join("\n", violations));
    }

    [Fact]
    [Trait("Phase", "M")]
    public void R_M2_Application_Should_Not_Know_Outer_Layers_Or_Game_Frameworks()
    {
        var violations = FindViolations("Application",
            @"using\s+RimMind\.Presentation",
            @"using\s+RimMind\.Infrastructure",
            @"using\s+Verse",
            @"using\s+UnityEngine",
            @"using\s+HarmonyLib",
            @"using\s+RimWorld");

        violations.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M2-NAMESPACE: Application must not know outer layers or game frameworks. Violations:\n{0}",
            string.Join("\n", violations));
    }

    [Fact]
    [Trait("Phase", "M")]
    public void R_M2_Presentation_Should_Only_Know_Infrastructure_In_CompositionRoot()
    {
        var violations = new List<string>();
        foreach (string file in CsFiles("Presentation"))
        {
            string fileName = Path.GetFileName(file);
            if (fileName.Equals("RimMindCompositionRoot.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"using\s+RimMind\.Infrastructure", RegexOptions.Multiline))
            {
                string rel = file.Substring(SourceDir.Length + 1);
                violations.Add(rel);
            }
        }

        violations.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M2-NAMESPACE: Presentation may depend on Infrastructure only in RimMindCompositionRoot.cs. Violations:\n{0}",
            string.Join("\n", violations));
    }

    [Fact]
    [Trait("Phase", "M")]
    public void R_M2_Public_Api_Should_Not_Open_Concrete_Verse_Windows()
    {
        var violations = new List<string>();
        foreach (string file in CsFiles(Path.Combine("Presentation", "Api")))
        {
            string text = File.ReadAllText(file);
            if (text.Contains("Find.WindowStack") || text.Contains("Window_RimMind"))
            {
                string rel = file.Substring(SourceDir.Length + 1);
                violations.Add(rel);
            }
        }

        violations.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M2-NAMESPACE: Presentation/Api must call runtime/window abstractions, not concrete Verse windows. Violations:\n{0}",
            string.Join("\n", violations));
    }
}
