using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G8_NoPublicInterfacesInApplicationTests
    {
        [Fact]
        [Trait("Phase", "G")]
        public void R_G8_Application_Public_Interfaces_Should_Be_In_Common_Interfaces_Only()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var applicationDir = Path.Combine(sourceDir, "Application");
            Directory.Exists(applicationDir).Should().BeTrue("Application directory must exist");

            var violatingFiles = new List<string>();
            var commonInterfacesDir = Path.Combine("Application", "Common", "Interfaces");

            foreach (var file in Directory.GetFiles(applicationDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (relativePath.StartsWith(commonInterfacesDir + Path.DirectorySeparatorChar) ||
                    relativePath.StartsWith(commonInterfacesDir + Path.AltDirectorySeparatorChar))
                    continue;

                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, @"public\s+interface\s+I"))
                {
                    violatingFiles.Add(relativePath);
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-G8: Application/ public interfaces must reside in Application/Common/Interfaces/ only. " +
                "Per Jason Taylor Clean Architecture, Application/Common/Interfaces/ is the correct location for port/adapter interfaces. " +
                "Other Application subdirectories should not define public interfaces. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G8_NoPublicInterfacesInApplicationTests).Assembly.Location);
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
