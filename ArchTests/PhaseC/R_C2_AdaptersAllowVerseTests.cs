using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class AdaptersAllowVerseTests
    {
        [Fact]
        [Trait("Phase", "C")]
        public void R_C2_Adapters_VerseLayer_ShouldExist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var adaptersVerseDir = Path.Combine(sourceDir, "Adapters", "Verse");
            Directory.Exists(adaptersVerseDir).Should().BeTrue(
                "Adapters/Verse directory must exist as the designated layer for Verse/RimWorld interactions.");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C2_Adapters_VerseFiles_ShouldUse_VerseNamespace()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var adaptersVerseDir = Path.Combine(sourceDir, "Adapters", "Verse");
            if (!Directory.Exists(adaptersVerseDir)) return;

            var filesWithVerse = new List<string>();
            var verseUsingPattern = @"using\s+Verse\s*;";

            foreach (var file in Directory.GetFiles(adaptersVerseDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, verseUsingPattern))
                {
                    filesWithVerse.Add(Path.GetFileName(file));
                }
            }

            filesWithVerse.Should().NotBeEmpty(
                "Adapters/Verse is the designated layer for Verse interactions. " +
                "At least one file should use 'using Verse;' to demonstrate this is the correct location.");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C2_Adapters_Namespace_ShouldBe_RimMind_Adapters()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var adaptersDir = Path.Combine(sourceDir, "Adapters");
            if (!Directory.Exists(adaptersDir)) return;

            var violatingFiles = new List<string>();
            var expectedNsPattern = @"namespace\s+RimMind\.Adapters";

            foreach (var file in Directory.GetFiles(adaptersDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (!Regex.IsMatch(source, expectedNsPattern))
                {
                    var relativePath = file.Substring(adaptersDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    violatingFiles.Add($"Adapters/{relativePath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "All files in Adapters/ must use RimMind.Adapters.* namespace. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(AdaptersAllowVerseTests).Assembly.Location);
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
