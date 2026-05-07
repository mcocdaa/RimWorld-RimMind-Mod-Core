using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class ContractsPureTests
    {
        private static readonly string[] ForbiddenUsingPatterns = new[]
        {
            @"using\s+RimMind\.Kernel\s*;",
            @"using\s+RimMind\.Kernel\.\w+\s*;",
            @"using\s+RimMind\.Adapters\s*;",
            @"using\s+RimMind\.Adapters\.\w+\s*;",
            @"using\s+Newtonsoft\.Json\s*;",
            @"using\s+HarmonyLib\s*;",
            @"using\s+Verse\s*;",
            @"using\s+Verse\.\w+\s*;",
            @"using\s+RimWorld\s*;",
            @"using\s+RimWorld\.\w+\s*;",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C3_Contracts_ShouldNot_Import_Kernel_Or_Adapters()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var contractsDir = Path.Combine(sourceDir, "Contracts");
            Directory.Exists(contractsDir).Should().BeTrue("Contracts directory must exist");
            Directory.GetFiles(contractsDir, "*.cs", SearchOption.AllDirectories).Should().NotBeEmpty(
                "Contracts directory must contain at least one .cs file");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(contractsDir, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(contractsDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var source = File.ReadAllText(file);

                foreach (var pattern in ForbiddenUsingPatterns)
                {
                    if (Regex.IsMatch(source, pattern, RegexOptions.Multiline))
                    {
                        var match = Regex.Match(source, pattern, RegexOptions.Multiline);
                        violatingFiles.Add($"Contracts/{relativePath} (found: {match.Value.Trim()})");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C3: Contracts namespace must be pure — no dependencies on Kernel, Adapters, Verse, RimWorld, Newtonsoft.Json, or HarmonyLib. " +
                "Contracts define interfaces and data types only. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C3_Contracts_Namespace_ShouldBe_RimMind_Contracts()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var contractsDir = Path.Combine(sourceDir, "Contracts");
            if (!Directory.Exists(contractsDir)) return;

            var violatingFiles = new List<string>();
            var expectedNsPattern = @"namespace\s+RimMind\.Contracts";

            foreach (var file in Directory.GetFiles(contractsDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (!Regex.IsMatch(source, expectedNsPattern))
                {
                    var relativePath = file.Substring(contractsDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    violatingFiles.Add($"Contracts/{relativePath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C3: All files in Contracts/ directory must use RimMind.Contracts.* namespace. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C3_Contracts_ShouldContain_OnlyInterfaces_And_Enums()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var contractsDir = Path.Combine(sourceDir, "Contracts");
            if (!Directory.Exists(contractsDir)) return;

            var violatingFiles = new List<string>();
            var classPattern = @"(?:public|internal)\s+(?:sealed\s+|abstract\s+)?class\s+";
            var allowedClassPattern = @"class\s+\w+Attribute\s*:\s*Attribute|class\s+\w+Event\s*\{|class\s+\w+Dto\s*\{|class\s+\w+Data\s*\{";

            foreach (var file in Directory.GetFiles(contractsDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, classPattern) && !Regex.IsMatch(source, allowedClassPattern))
                {
                    var relativePath = file.Substring(contractsDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    violatingFiles.Add($"Contracts/{relativePath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C3: Contracts should contain only interfaces, enums, records, attributes, and pure data DTOs — no behavioral class implementations. " +
                "Move implementation classes to Kernel or Adapters. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(ContractsPureTests).Assembly.Location);
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
