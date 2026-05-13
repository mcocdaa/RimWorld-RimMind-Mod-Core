using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class PatchLocationTests
    {
        [Fact]
        [Trait("Phase", "C")]
        public void R_C5_PatchClasses_MustBeIn_Infrastructure_Patches()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var violatingFiles = new List<string>();
            var harmonyPatchPattern = @"\[HarmonyPatch";

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (relativePath.StartsWith("Infrastructure" + Path.DirectorySeparatorChar + "Patches" + Path.DirectorySeparatorChar)) continue;

                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, harmonyPatchPattern))
                {
                    violatingFiles.Add(relativePath);
                }
            }

            violatingFiles.Should().BeEmpty(
                "All HarmonyPatch classes must reside in Source/Infrastructure/Patches/. " +
                "This ensures all Harmony transpilers/prefixes/postfixes are isolated in the Infrastructure layer. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C5_Infrastructure_Patches_Directory_ShouldExist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var patchesDir = Path.Combine(sourceDir, "Infrastructure", "Patches");
            Directory.Exists(patchesDir).Should().BeTrue(
                "Infrastructure/Patches directory must exist as the designated location for all HarmonyPatch classes.");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(PatchLocationTests).Assembly.Location);
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
