using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class InfrastructureAllowVerseTests
    {
        [Fact]
        [Trait("Phase", "C")]
        public void R_C2_Infrastructure_VerseLayer_ShouldExist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var infrastructureVerseDir = Path.Combine(sourceDir, "Infrastructure", "Verse");
            Directory.Exists(infrastructureVerseDir).Should().BeTrue(
                "Infrastructure/Verse directory must exist as the designated layer for Verse/RimWorld interactions.");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C2_Infrastructure_VerseFiles_ShouldUse_VerseNamespace()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var infrastructureVerseDir = Path.Combine(sourceDir, "Infrastructure", "Verse");
            if (!Directory.Exists(infrastructureVerseDir)) return;

            var filesWithVerse = new List<string>();
            var verseUsingPattern = @"using\s+Verse\s*;";

            foreach (var file in Directory.GetFiles(infrastructureVerseDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, verseUsingPattern))
                {
                    filesWithVerse.Add(Path.GetFileName(file));
                }
            }

            filesWithVerse.Should().NotBeEmpty(
                "Infrastructure/Verse is the designated layer for Verse interactions. " +
                "At least one file should use 'using Verse;' to demonstrate this is the correct location.");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C2_Infrastructure_Namespace_ShouldBe_RimMind_Infrastructure()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var infrastructureDir = Path.Combine(sourceDir, "Infrastructure");
            if (!Directory.Exists(infrastructureDir)) return;

            var violatingFiles = new List<string>();
            var expectedNsPattern = @"namespace\s+RimMind\.Infrastructure";

            foreach (var file in Directory.GetFiles(infrastructureDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (!Regex.IsMatch(source, expectedNsPattern))
                {
                    var relativePath = file.Substring(infrastructureDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    violatingFiles.Add($"Infrastructure/{relativePath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "All files in Infrastructure/ must use RimMind.Infrastructure.* namespace. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(InfrastructureAllowVerseTests).Assembly.Location);
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
