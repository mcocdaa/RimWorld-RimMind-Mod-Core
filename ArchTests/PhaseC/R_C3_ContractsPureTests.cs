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
            if (!Directory.Exists(contractsDir)) return;

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
                "Contracts namespace must be pure — no dependencies on Kernel, Adapters, Verse, RimWorld, Newtonsoft.Json, or HarmonyLib. " +
                "Contracts define interfaces and data types only. " +
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
