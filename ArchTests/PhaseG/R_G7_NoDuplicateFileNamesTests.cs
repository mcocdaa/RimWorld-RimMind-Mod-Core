using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G7_NoDuplicateFileNamesTests
    {
        private static readonly string[] Whitelist = new[]
        {
            "IsExternalInit.cs",
            "AIRequestContext.cs",
            "NpcChatContext.cs",
            "ContextBuildContext.cs",
            "DependencyInjection.cs",
        };

        [Fact]
        [Trait("Phase", "G")]
        public void R_G7_No_Duplicate_CS_File_Names()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var fileLocations = new Dictionary<string, List<string>>();

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "backup" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "backup_dead_code" + Path.DirectorySeparatorChar)))
            {
                var fileName = Path.GetFileName(file);
                if (Whitelist.Contains(fileName)) continue;

                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (!fileLocations.ContainsKey(fileName))
                    fileLocations[fileName] = new List<string>();

                fileLocations[fileName].Add(relativePath);
            }

            var duplicates = fileLocations
                .Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")
                .ToList();

            duplicates.Should().BeEmpty(
                "R-G7: No duplicate .cs file names allowed (except whitelisted polyfills). " +
                $"Duplicates:\n  {string.Join("\n  ", duplicates)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G7_NoDuplicateFileNamesTests).Assembly.Location);
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
