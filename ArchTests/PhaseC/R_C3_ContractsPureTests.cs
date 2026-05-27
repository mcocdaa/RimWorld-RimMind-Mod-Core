using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class DomainPureTests
    {
        private static readonly string[] ForbiddenUsingPatterns = new[]
        {
            @"using\s+RimMind\.Application\s*;",
            @"using\s+RimMind\.Application\.\w+\s*;",
            @"using\s+RimMind\.Infrastructure\s*;",
            @"using\s+RimMind\.Infrastructure\.\w+\s*;",
            @"using\s+Newtonsoft\.Json\s*;",
            @"using\s+HarmonyLib\s*;",
            @"using\s+Verse\s*;",
            @"using\s+Verse\.\w+\s*;",
            @"using\s+RimWorld\s*;",
            @"using\s+RimWorld\.\w+\s*;",
        };

        private static IEnumerable<string> GetSourceFiles(string dir)
        {
            return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "backup" + Path.DirectorySeparatorChar)
                         && !Path.GetFileName(f).Equals("IsExternalInit.cs", StringComparison.OrdinalIgnoreCase)
                         && !Path.GetFileName(f).Equals("NetStandardCompat.cs", StringComparison.OrdinalIgnoreCase));
        }

        private static readonly HashSet<string> KnownImportViolations = new(StringComparer.OrdinalIgnoreCase)
        {
        };

        private static readonly HashSet<string> KnownClassViolations = new(StringComparer.OrdinalIgnoreCase)
        {
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C3_Domain_ShouldNot_Import_Application_Or_Infrastructure()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var domainDir = Path.Combine(sourceDir, "Domain");
            Directory.Exists(domainDir).Should().BeTrue("Domain directory must exist");
            GetSourceFiles(domainDir).Should().NotBeEmpty(
                "Domain directory must contain at least one .cs file");

            var violatingFiles = new List<string>();

            foreach (var file in GetSourceFiles(domainDir))
            {
                var relativePath = file.Substring(domainDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (KnownImportViolations.Contains(relativePath)) continue;

                var source = File.ReadAllText(file);

                foreach (var pattern in ForbiddenUsingPatterns)
                {
                    if (Regex.IsMatch(source, pattern, RegexOptions.Multiline))
                    {
                        var match = Regex.Match(source, pattern, RegexOptions.Multiline);
                        violatingFiles.Add($"Domain/{relativePath} (found: {match.Value.Trim()})");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C3: Domain namespace must be pure — no dependencies on Application, Infrastructure, Verse, RimWorld, Newtonsoft.Json, or HarmonyLib. " +
                "Domain defines interfaces and data types only. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C3_Domain_Namespace_ShouldBe_RimMind_Domain()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var domainDir = Path.Combine(sourceDir, "Domain");
            if (!Directory.Exists(domainDir)) return;

            var violatingFiles = new List<string>();
            var expectedNsPattern = @"namespace\s+RimMind\.Domain";

            foreach (var file in GetSourceFiles(domainDir))
            {
                var source = File.ReadAllText(file);
                if (!Regex.IsMatch(source, expectedNsPattern))
                {
                    var relativePath = file.Substring(domainDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    violatingFiles.Add($"Domain/{relativePath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C3: All files in Domain/ directory must use RimMind.Domain.* namespace. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C3_Domain_ShouldContain_OnlyInterfaces_And_Enums()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var domainDir = Path.Combine(sourceDir, "Domain");
            if (!Directory.Exists(domainDir)) return;

            var violatingFiles = new List<string>();
            var classPattern = @"(?:public|internal)\s+(?!abstract\s+)(?:sealed\s+)?class\s+";
            var allowedClassPattern = @"class\s+\w+Attribute\s*:\s*Attribute|class\s+\w+Event\s*[{:]|class\s+\w+Dto\s*[{:]|class\s+\w+Data\s*[{:]|class\s+\w+Result\s*[{:]|class\s+\w+Request\s*[{:]|class\s+\w+Response\s*[{:]|class\s+\w+Tool\s*[{:]|class\s+\w+Context\s*[{:]|class\s+\w+Entry\s*[{:]|class\s+\w+Profile\s*[{:]|class\s+\w+Command\s*[{:]|class\s+\w+Message\s*[{:]|class\s+\w+Exception\s*[{:]|class\s+\w+Error\s*[{:]|class\s+\w+Errors\s*[{:]|class\s+\w+Cache\s*[{:]|class\s+\w+Meta\s*[{:]|class\s+\w+Diff\s*[{:]|class\s+\w+Layer\s*[{:]|class\s+\w+Code\s*[{:]|class\s+\w+Call\s*[{:]|class\s+\w+Envelope\s*[{:]|class\s+\w+Builder\s*[{:]|class\s+\w+Settings\s*[{:]|static\s+class\s+";

            foreach (var file in GetSourceFiles(domainDir))
            {
                var relPath = file.Substring(domainDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (KnownClassViolations.Contains(relPath)) continue;

                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, classPattern) && !Regex.IsMatch(source, allowedClassPattern))
                {
                    violatingFiles.Add($"Domain/{relPath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C3: Domain should contain only interfaces, enums, records, attributes, value objects, events, exceptions, and pure data DTOs — no behavioral class implementations. " +
                "Move implementation classes to Application or Infrastructure. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(DomainPureTests).Assembly.Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "RimMind-Core", "Source");
                if (Directory.Exists(candidate)) return candidate;

                candidate = Path.Combine(dir, "Source");
                if (Directory.Exists(candidate)) return candidate;

                dir = Directory.GetParent(dir)?.FullName;
            }
            return "";
        }
    }
}
