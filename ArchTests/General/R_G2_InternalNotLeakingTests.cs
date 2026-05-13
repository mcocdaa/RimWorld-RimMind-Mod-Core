using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.General
{
    public class InternalNotLeakingTests
    {
        private static readonly string[] AllowedTestAssemblies = new[]
        {
            "RimMindCore.Tests",
            "RimMindCore.ArchTests"
        };

        [Fact]
        [Trait("Phase", "General")]
        public void R_G2_InternalsVisibleTo_ShouldOnlyTarget_TestProjects()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var csprojFiles = new[]
            {
                Path.Combine(sourceDir, "RimMindCore.csproj"),
                Path.Combine(sourceDir, "Domain", "RimMindCore.Domain.csproj"),
                Path.Combine(sourceDir, "Application", "RimMindCore.Application.csproj"),
            };

            var violatingEntries = new List<string>();
            var ivtPattern = @"InternalsVisibleTo\s*\(\s*""([^""]+)""\s*\)";
            var ivtCsPattern = @"assembly:\s*InternalsVisibleTo\s*\(\s*""([^""]+)""\s*\)";

            foreach (var csproj in csprojFiles)
            {
                if (!File.Exists(csproj)) continue;

                var csprojDir = Path.GetDirectoryName(csproj)!;

                foreach (var csFile in Directory.GetFiles(csprojDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                             && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
                {
                    var source = File.ReadAllText(csFile);

                    foreach (Match match in Regex.Matches(source, ivtCsPattern))
                    {
                        var assemblyName = match.Groups[1].Value;
                        if (!AllowedTestAssemblies.Any(ta => assemblyName.StartsWith(ta)))
                        {
                            var relativePath = csFile.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            violatingEntries.Add($"{relativePath}: InternalsVisibleTo(\"{assemblyName}\")");
                        }
                    }
                }

                var csprojSource = File.ReadAllText(csproj);
                foreach (Match match in Regex.Matches(csprojSource, ivtPattern))
                {
                    var assemblyName = match.Groups[1].Value;
                    if (!AllowedTestAssemblies.Any(ta => assemblyName.StartsWith(ta)))
                    {
                        violatingEntries.Add($"{Path.GetFileName(csproj)}: InternalsVisibleTo(\"{assemblyName}\")");
                    }
                }
            }

            violatingEntries.Should().BeEmpty(
                "R-G2: InternalsVisibleTo must only target test projects (RimMindCore.Tests, RimMindCore.ArchTests). " +
                "Allowing other assemblies to see internals creates hidden coupling. " +
                $"Violating entries:\n  {string.Join("\n  ", violatingEntries)}");
        }
    }
}
