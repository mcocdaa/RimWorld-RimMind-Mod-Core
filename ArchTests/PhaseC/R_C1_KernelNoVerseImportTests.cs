using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class KernelNoVerseImportTests
    {
        private static readonly string[] ForbiddenUsingPatterns = new[]
        {
            @"using\s+Verse\s*;",
            @"using\s+Verse\.AI\s*;",
            @"using\s+Verse\.Sound\s*;",
            @"using\s+Verse\.NoTest\s*;",
            @"using\s+RimWorld\s*;",
            @"using\s+RimWorld\.Planet\s*;",
        };

        private static readonly HashSet<string> AllowedFiles = new()
        {
            "AgentBusImpl.cs",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C1_Kernel_ShouldNot_Import_Verse_Or_RimWorld()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var kernelDir = Path.Combine(sourceDir, "Kernel");
            kernelDir.Should().NotBeNull("Kernel directory must exist");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(kernelDir, "*.cs", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (AllowedFiles.Contains(fileName)) continue;

                var relativePath = file.Substring(kernelDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var source = File.ReadAllText(file);

                foreach (var pattern in ForbiddenUsingPatterns)
                {
                    if (Regex.IsMatch(source, pattern, RegexOptions.Multiline))
                    {
                        var match = Regex.Match(source, pattern, RegexOptions.Multiline);
                        violatingFiles.Add($"Kernel/{relativePath} (found: {match.Value.Trim()})");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "Kernel namespace must not import Verse or RimWorld. " +
                "Only Verse.Pawn and Verse.IExposable may be used via fully-qualified names (no 'using Verse;'). " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(KernelNoVerseImportTests).Assembly.Location);
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
