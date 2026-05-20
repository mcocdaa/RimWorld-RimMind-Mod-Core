using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G6_PresentationSubdirsAllowlistTests
    {
        private static readonly string[] AllowedSubdirs = new[]
        {
            "Agent",
            "Api",
            "Context",
            "Llm",
            "Perception",
            "Pipeline",
            "Runtime",
            "Sensor",
            "Settings",
            "UI",
        };

        [Fact]
        [Trait("Phase", "G")]
        public void R_G6_Presentation_Subdirectories_Should_Match_Allowlist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var presentationDir = Path.Combine(sourceDir, "Presentation");
            Directory.Exists(presentationDir).Should().BeTrue("Presentation directory must exist");

            var actualSubdirs = Directory.GetDirectories(presentationDir)
                .Select(d => Path.GetFileName(d))
                .ToList();

            var unexpected = actualSubdirs.Where(d => !AllowedSubdirs.Contains(d)).ToList();

            unexpected.Should().BeEmpty(
                "R-G6: Source/Presentation/ should only contain allowed subdirectories: " +
                $"{string.Join(", ", AllowedSubdirs)}. " +
                $"Unexpected: {string.Join(", ", unexpected)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G6_PresentationSubdirsAllowlistTests).Assembly.Location);
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
