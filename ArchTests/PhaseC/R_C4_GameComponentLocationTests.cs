using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class GameComponentLocationTests
    {
        private static readonly string[] ComponentBaseClasses = new[]
        {
            "GameComponent",
            "WorldComponent",
            "MapComponent",
            "ThingComp",
            "JobDriver",
            "ThinkNode",
        };

        private static readonly HashSet<string> AllowedOutsideAdapters = new()
        {
            "VerseStubs.cs",
            "ArchTestStubs.cs",
            "NpcManager.cs",
            "SensorManager.cs",
            "AIDebugLog.cs",
            "RimMindRuntimeGameComponent.cs",
            "CompPawnAgent.cs",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C4_GameComponentClasses_MustBeIn_Infrastructure_Verse()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (AllowedOutsideAdapters.Contains(fileName)) continue;

                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (relativePath.StartsWith("Infrastructure" + Path.DirectorySeparatorChar)) continue;
                var source = File.ReadAllText(file);

                foreach (var baseClass in ComponentBaseClasses)
                {
                    var pattern = $@"\bclass\s+\w+\s*:\s*[^{{]*\b{Regex.Escape(baseClass)}\b";
                    if (Regex.IsMatch(source, pattern))
                    {
                        violatingFiles.Add($"{relativePath} (inherits {baseClass})");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "All GameComponent/WorldComponent/MapComponent/ThingComp classes must reside in Source/Infrastructure/Verse/. " +
                "This ensures Verse-dependent lifecycle hooks are isolated in the Infrastructure layer. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(GameComponentLocationTests).Assembly.Location);
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
