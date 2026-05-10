using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G4_NoDuplicateTypeNameTests
    {
        private static readonly string[] Whitelist = new[]
        {
            "IsExternalInit",
        };

        [Fact]
        [Trait("Phase", "G")]
        public void R_G4_No_Duplicate_Public_Type_Names()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var typeLocations = new Dictionary<string, List<string>>();

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            {
                var source = File.ReadAllText(file);
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                foreach (Match match in Regex.Matches(source, @"(?:public|internal)\s+(?:sealed\s+)?(?:class|interface|enum|struct)\s+(\w+)"))
                {
                    var typeName = match.Groups[1].Value;
                    if (Whitelist.Contains(typeName)) continue;

                    if (!typeLocations.ContainsKey(typeName))
                        typeLocations[typeName] = new List<string>();

                    typeLocations[typeName].Add(relativePath);
                }
            }

            var duplicates = typeLocations
                .Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")
                .ToList();

            duplicates.Should().BeEmpty(
                "R-G4: No duplicate public type names allowed (except whitelisted polyfills). " +
                $"Duplicates:\n  {string.Join("\n  ", duplicates)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G4_NoDuplicateTypeNameTests).Assembly.Location);
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
