using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G5_LegacyTopLevelDirsTests
    {
        private static readonly string[] ForbiddenDirs = new[]
        {
            "Client",
            "Comps",
            "Debug",
            "Settings",
        };

        [Fact]
        [Trait("Phase", "G")]
        public void R_G5_Legacy_TopLevel_Directories_Should_Not_Exist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var existing = ForbiddenDirs
                .Where(d => Directory.Exists(Path.Combine(sourceDir, d)))
                .ToList();

            existing.Should().BeEmpty(
                "R-G5: Legacy top-level directories (Client, Comps, Debug, Settings) must not exist. " +
                $"Found: {string.Join(", ", existing)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G5_LegacyTopLevelDirsTests).Assembly.Location);
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

    public class R_G5b_NpcOnlyStorageDriversTests
    {
        private static readonly string[] AllowedFiles = new[]
        {
            "HybridStorageDriver.cs",
            "LocalStorageDriver.cs",
            "Player2StorageDriver.cs",
            "StorageDriverFactory.cs",
        };

        [Fact]
        [Trait("Phase", "G")]
        public void R_G5b_Npc_Directory_Should_Only_Contain_StorageDrivers()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var npcDir = Path.Combine(sourceDir, "Npc");
            if (!Directory.Exists(npcDir)) return;

            var files = Directory.GetFiles(npcDir, "*.cs")
                .Select(f => Path.GetFileName(f))
                .ToList();

            var unexpected = files.Where(f => !AllowedFiles.Contains(f)).ToList();

            unexpected.Should().BeEmpty(
                "R-G5b: Source/Npc/ should only contain StorageDriver files. " +
                $"Unexpected files: {string.Join(", ", unexpected)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G5b_NpcOnlyStorageDriversTests).Assembly.Location);
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
