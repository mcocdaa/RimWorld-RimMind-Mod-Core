using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_C1_PipelineMiddlewareNamespaceTests
    {
        [Fact]
        [Trait("Phase", "G")]
        public void All_Pipeline_Middlewares_Should_Be_In_Application_Namespace()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var pipelineDirs = new[]
            {
                Path.Combine(sourceDir, "Application", "Features", "Pipeline"),
                Path.Combine(sourceDir, "Application", "Common", "Behaviours"),
            };

            var violatingFiles = new List<string>();

            foreach (var pipelineDir in pipelineDirs)
            {
                if (!Directory.Exists(pipelineDir)) continue;

                foreach (var file in Directory.GetFiles(pipelineDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                             && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
                {
                    var source = File.ReadAllText(file);
                    var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (!Regex.IsMatch(source, @"namespace\s+RimMind\.Application"))
                    {
                        violatingFiles.Add(relativePath);
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C1 enhanced: All Pipeline middleware files in Application/ must use RimMind.Application.* namespace. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_C1_PipelineMiddlewareNamespaceTests).Assembly.Location);
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
