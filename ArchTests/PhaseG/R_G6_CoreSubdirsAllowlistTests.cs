using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G6_CoreSubdirsAllowlistTests
    {
        private static readonly string[] AllowedSubdirs = new[]
        {
            "Agent",
            "Perception",
            "Runtime",
            "Sensor",
        };

        [Fact]
        [Trait("Phase", "G")]
        public void R_G6_Core_Subdirectories_Should_Match_Allowlist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var coreDir = Path.Combine(sourceDir, "Core");
            Directory.Exists(coreDir).Should().BeTrue("Core directory must exist");

            var actualSubdirs = Directory.GetDirectories(coreDir)
                .Select(d => Path.GetFileName(d))
                .ToList();

            var unexpected = actualSubdirs.Where(d => !AllowedSubdirs.Contains(d)).ToList();

            unexpected.Should().BeEmpty(
                "R-G6: Source/Core/ should only contain allowed subdirectories: " +
                $"{string.Join(", ", AllowedSubdirs)}. " +
                $"Unexpected: {string.Join(", ", unexpected)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G6_CoreSubdirsAllowlistTests).Assembly.Location);
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
