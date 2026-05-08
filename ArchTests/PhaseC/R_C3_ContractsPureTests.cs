using System;
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

        private static IEnumerable<string> GetSourceFiles(string dir)
        {
            return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !Path.GetFileName(f).Equals("IsExternalInit.cs", StringComparison.OrdinalIgnoreCase));
        }

        private static readonly HashSet<string> KnownImportViolations = new(StringComparer.OrdinalIgnoreCase)
        {
            @"Settings\ContextSettings.cs",
            @"Settings\RimMindCoreSettings.cs",
        };

        private static readonly HashSet<string> KnownClassViolations = new(StringComparer.OrdinalIgnoreCase)
        {
            @"Context\BudgetSchedulerConfig.cs",
            @"Context\PromptBudget.cs",
            @"Flywheel\IAnalysisReportWriter.cs",
            @"Prompt\PromptSection.cs",
            @"Settings\ContextSettings.cs",
            @"Settings\RimMindCoreSettings.cs",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C3_Contracts_ShouldNot_Import_Kernel_Or_Adapters()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var contractsDir = Path.Combine(sourceDir, "Contracts");
            Directory.Exists(contractsDir).Should().BeTrue("Contracts directory must exist");
            GetSourceFiles(contractsDir).Should().NotBeEmpty(
                "Contracts directory must contain at least one .cs file");

            var violatingFiles = new List<string>();

            foreach (var file in GetSourceFiles(contractsDir))
            {
                var relativePath = file.Substring(contractsDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (KnownImportViolations.Contains(relativePath)) continue;

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

            foreach (var file in GetSourceFiles(contractsDir))
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
            var classPattern = @"(?:public|internal)\s+(?!abstract\s+)(?:sealed\s+)?class\s+";
            var allowedClassPattern = @"class\s+\w+Attribute\s*:\s*Attribute|class\s+\w+Event\s*\{|class\s+\w+Dto\s*\{|class\s+\w+Data\s*\{|class\s+\w+Result\s*\{|class\s+\w+Request\s*\{|class\s+\w+Response\s*\{|class\s+\w+Tool\s*\{|class\s+\w+Context\s*\{|class\s+\w+Entry\s*\{|class\s+\w+Profile\s*\{|class\s+\w+Command\s*\{|class\s+\w+Message\s*\{";

            foreach (var file in GetSourceFiles(contractsDir))
            {
                var relPath = file.Substring(contractsDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (KnownClassViolations.Contains(relPath)) continue;

                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, classPattern) && !Regex.IsMatch(source, allowedClassPattern))
                {
                    violatingFiles.Add($"Contracts/{relPath}");
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
