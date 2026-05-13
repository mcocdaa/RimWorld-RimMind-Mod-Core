using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.General
{
    public class PublicTypesInDomainTests
    {
        private static readonly string[] ValidNamespacePrefixes = new[]
        {
            "RimMind.Presentation",
            "RimMind.Domain",
            "RimMind.Application",
            "RimMind.Infrastructure",
        };

        private static readonly HashSet<string> WhitelistFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "RimMindCoreMod.cs",
            "RimMindAPI.cs",
            "VerseStubs.cs",
            "NetStandardCompat.cs",
        };

        [Fact]
        [Trait("Phase", "General")]
        public void R_G1_PublicTypes_ShouldResideIn_ValidNamespaces()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var violatingFiles = new List<string>();
            var publicClassPattern = @"(?:public|internal)\s+(?!abstract\s+)(?:sealed\s+)?(?:class|record|struct)\s+(\w+)";
            var namespacePattern = @"namespace\s+([\w.]+)";

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "backup" + Path.DirectorySeparatorChar)))
            {
                var fileName = Path.GetFileName(file);
                if (WhitelistFiles.Contains(fileName)) continue;

                var source = File.ReadAllText(file);
                if (!Regex.IsMatch(source, publicClassPattern)) continue;

                var nsMatch = Regex.Match(source, namespacePattern);
                if (!nsMatch.Success) continue;

                var ns = nsMatch.Groups[1].Value;
                var isValid = ValidNamespacePrefixes.Any(prefix => ns.StartsWith(prefix + ".") || ns == prefix);

                if (!isValid)
                {
                    var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    violatingFiles.Add($"{relativePath} (namespace: {ns})");
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-G1: All public types must reside in RimMind.Presentation.*, RimMind.Domain.*, RimMind.Application.*, or RimMind.Infrastructure.* namespaces. " +
                "Internal implementation namespaces should not leak public types. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }
    }
}
