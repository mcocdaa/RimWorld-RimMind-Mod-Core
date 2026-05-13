using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G3_NoLegacyPipelineDirTests
    {
        [Fact]
        [Trait("Phase", "G")]
        public void R_G3_Legacy_Contracts_Kernel_Core_Adapters_Directories_Should_Not_Exist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var legacyDirs = new[]
            {
                Path.Combine(sourceDir, "Contracts"),
                Path.Combine(sourceDir, "Kernel"),
                Path.Combine(sourceDir, "Core"),
                Path.Combine(sourceDir, "Adapters"),
            };

            var existingLegacyDirs = legacyDirs.Where(d => Directory.Exists(d)).ToList();

            existingLegacyDirs.Should().BeEmpty(
                "R-G3: Legacy directories (Contracts, Kernel, Core, Adapters) must not exist under Source/. " +
                "The project has been restructured to Jason Taylor CleanArchitecture (Domain/Application/Infrastructure/Presentation). " +
                $"Existing legacy dirs: {string.Join(", ", existingLegacyDirs)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G3_NoLegacyPipelineDirTests).Assembly.Location);
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
