using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseB
{
    public class ExtensionsOnlyEntryTests
    {
        private static readonly string[] LegacyRegistrationPatterns = new[]
        {
            @"RimMindAPI\.RegisterSettingsTab\s*\(",
            @"RimMindAPI\.RegisterToggleBehavior\s*\(",
            @"RimMindAPI\.RegisterDialogueSkipCheck\s*\(",
            @"RimMindAPI\.RegisterFloatMenuSkipCheck\s*\(",
            @"RimMindAPI\.RegisterActionSkipCheck\s*\(",
            @"RimMindAPI\.RegisterStorytellerIncidentSkipCheck\s*\(",
            @"RimMindAPI\.RegisterModCooldown\s*\(",
            @"RimMindAPI\.RegisterDialogueTrigger\s*\(",
            @"RimMindAPI\.RegisterIncidentExecutedCallback\s*\("
        };

        [Fact]
        [Trait("Phase", "B")]
        public void R_B3_NoLegacyRegistrationCalls_InSource()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var source = File.ReadAllText(file);

                foreach (var pattern in LegacyRegistrationPatterns)
                {
                    if (Regex.IsMatch(source, pattern))
                    {
                        violatingFiles.Add($"{relativePath} (matches: {pattern})");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                $"All extension registration must use RimMindAPI.Extensions<T>().Register(impl) instead of legacy stringly-typed methods. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "B")]
        public void R_B3_ExtensionsMethod_ExistsInRimMindAPI()
        {
            var sourcePath = FindFileUpwards("RimMindAPI.cs");
            File.Exists(sourcePath).Should().BeTrue("RimMindAPI.cs source file must exist for analysis");

            var source = File.ReadAllText(sourcePath);
            source.Should().MatchRegex(@"public\s+static\s+IExtensionRegistry<T>\s+Extensions<T>\s*\(\s*\)",
                "RimMindAPI must expose the Extensions<T>() method as the unified extension registration entry point.");
        }

        [Fact]
        [Trait("Phase", "B")]
        public void R_B3_Submodules_UseExtensionsForRegistration()
        {
            var projectRoot = FindProjectRoot();
            if (string.IsNullOrEmpty(projectRoot)) return;

            var submoduleDirs = new[]
            {
                "RimMind-Advisor",
                "RimMind-Memory",
                "RimMind-Personality",
                "RimMind-Dialogue",
                "RimMind-Storyteller",
                "RimMind-Bridge-RimChat",
                "RimMind-Bridge-RimTalk",
                "RimMind-Actions"
            };

            var violatingFiles = new List<string>();

            foreach (var submoduleName in submoduleDirs)
            {
                var subSourceDir = Path.Combine(projectRoot, submoduleName, "Source");
                if (!Directory.Exists(subSourceDir)) continue;

                foreach (var file in Directory.GetFiles(subSourceDir, "*.cs", SearchOption.AllDirectories))
                {
                    var relativePath = file.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var source = File.ReadAllText(file);

                    foreach (var pattern in LegacyRegistrationPatterns)
                    {
                        if (Regex.IsMatch(source, pattern))
                        {
                            violatingFiles.Add($"{relativePath}");
                            break;
                        }
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                $"All submodules must use RimMindAPI.Extensions<T>().Register(impl) for extension registration. " +
                $"Legacy stringly-typed registration methods have been removed. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(ExtensionsOnlyEntryTests).Assembly.Location);
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

        private static string FindFileUpwards(string fileName)
        {
            var dir = Path.GetDirectoryName(typeof(ExtensionsOnlyEntryTests).Assembly.Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "RimMind-Core", "Source", fileName);
                if (File.Exists(candidate)) return candidate;

                candidate = Path.Combine(dir, "Source", fileName);
                if (File.Exists(candidate)) return candidate;

                dir = Directory.GetParent(dir)?.FullName;
            }
            return fileName;
        }

        private static string FindProjectRoot()
        {
            var dir = Path.GetDirectoryName(typeof(ExtensionsOnlyEntryTests).Assembly.Location);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir, "RimMind-Core")) &&
                    Directory.Exists(Path.Combine(dir, "RimMind-Core", "Source")))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }
            return "";
        }
    }
}
