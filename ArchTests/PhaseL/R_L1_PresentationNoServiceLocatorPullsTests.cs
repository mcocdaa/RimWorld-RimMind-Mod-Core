using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseL
{
    /// <summary>
    /// R-L1: Presentation layer files (excluding Composition Root) must not
    /// pull services from RimMindServiceLocator. Use constructor injection instead.
    /// Registration (RimMindServiceLocator.Register) is fine; only Get/TryGet are violations.
    /// </summary>
    public class R_L1_PresentationNoServiceLocatorPullsTests
    {
        private static readonly HashSet<string> WhitelistFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RimMindCompositionRoot.cs",
            "RimMindRuntime.cs",
            "RimMindLifecycleManager.cs",
            "RimMindExtensionManager.cs"
        };

        [Fact]
        [Trait("Phase", "L")]
        public void R_L1_Presentation_Files_Should_Not_Pull_ServiceLocator()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist");

            var presentationDir = Path.Combine(sourceDir, "Presentation");
            Directory.Exists(presentationDir).Should().BeTrue("Presentation directory must exist");

            var violations = new List<string>();
            var csFiles = Directory.GetFiles(presentationDir, "*.cs", SearchOption.AllDirectories);

            foreach (var file in csFiles)
            {
                if (WhitelistFiles.Contains(Path.GetFileName(file))) continue;

                var relativePath = file.Substring(sourceDir.Length + 1);
                if (relativePath.StartsWith(@"Presentation\Runtime\Composition\", StringComparison.OrdinalIgnoreCase)) continue;

                var source = File.ReadAllText(file);

                // Check for SL pulls (Get/TryGet) but not Register
                if (Regex.IsMatch(source, @"RimMindServiceLocator\.(Get|TryGet)<"))
                {
                    violations.Add(relativePath);
                }
            }

            violations.Should().BeEmpty(
                "R-L1: Presentation layer files (except Composition Root) should not pull services " +
                "from ServiceLocator. Use constructor injection instead. Violating files:\n" +
                string.Join("\n", violations));
        }
    }
}
