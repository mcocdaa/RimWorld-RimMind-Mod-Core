using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G8_NoPublicInterfacesInKernelTests
    {
        [Fact]
        [Trait("Phase", "G")]
        public void R_G8_Kernel_Should_Not_Define_Public_Interfaces()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var kernelDir = Path.Combine(sourceDir, "Kernel");
            Directory.Exists(kernelDir).Should().BeTrue("Kernel directory must exist");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(kernelDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, @"public\s+interface\s+I"))
                {
                    var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    violatingFiles.Add(relativePath);
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-G8: Kernel/ must not define any public interfaces. " +
                "All public interfaces belong in Contracts/. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G8_NoPublicInterfacesInKernelTests).Assembly.Location);
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
