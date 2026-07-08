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
            "FlywheelGameComponent.cs",
            "FlywheelParameterStoreGameComponent.cs",
            "AgentBusGameComponent.cs",
            "AIRequestQueueGameComponent.cs",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C4_GameComponentClasses_MustBeIn_Infrastructure_Verse()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "backup" + Path.DirectorySeparatorChar)))
            {
                var fileName = Path.GetFileName(file);
                if (AllowedOutsideAdapters.Contains(fileName)) continue;

                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (relativePath.StartsWith("Infrastructure" + Path.DirectorySeparatorChar)) continue;
                if (relativePath.StartsWith("Presentation" + Path.DirectorySeparatorChar + "Runtime" + Path.DirectorySeparatorChar)) continue;
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

        [Fact]
        [Trait("Phase", "C")]
        public void EmbedCache_Should_Be_In_Infrastructure_Folder()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var infrastructurePath = Path.Combine(sourceDir, "Infrastructure", "Cache", "EmbedCache.cs");
            var domainPath = Path.Combine(sourceDir, "Domain", "ValueObjects", "EmbedCache.cs");

            File.Exists(infrastructurePath).Should().BeTrue(
                "EmbedCache is a stateful cache with locks and LRU eviction - an Infrastructure concern, not a Domain ValueObject. " +
                "It should reside in Source/Infrastructure/Cache/.");

            File.Exists(domainPath).Should().BeFalse(
                "EmbedCache should no longer be in Source/Domain/ValueObjects/.");
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
