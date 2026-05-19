using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseE
{
    public class TelemetryOnlyInMiddlewareTests
    {
        private static readonly string[] TelemetryRecordPatterns = new[]
        {
            @"\.Telemetry\.Record\s*\(",
            @"Telemetry\.Record\s*\(",
        };

        private static readonly HashSet<string> AllowedFiles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            @"Application\Common\Behaviours\CommonTelemetryMiddleware.cs",
            @"Application\Features\Pipeline\Bus\BusPublishTelemetryMiddleware.cs",
            @"Application\Features\Pipeline\Context\ContextBuildTelemetryMiddleware.cs",
            @"Presentation\Pipeline\AI\AIRequestPipelineFactory.cs",
            @"Presentation\Pipeline\Npc\NpcChatPipelineFactory.cs",
            @"Presentation\Pipeline\Context\ContextBuildPipelineFactory.cs",
            @"Application\Features\Pipeline\Bus\BusPublishPipelineFactory.cs",
        };

        private static readonly HashSet<string> KnownViolations = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
        };

        [Fact]
        [Trait("Phase", "E")]
        public void R_E1_Telemetry_Record_Should_Only_Be_Called_In_TelemetryMiddleware()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "backup" + Path.DirectorySeparatorChar)))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (AllowedFiles.Contains(relativePath))
                    continue;

                if (KnownViolations.Contains(relativePath))
                    continue;

                if (KnownViolations.Contains(Path.GetFileName(relativePath)))
                    continue;

                var source = File.ReadAllText(file);

                foreach (var pattern in TelemetryRecordPatterns)
                {
                    if (Regex.IsMatch(source, pattern))
                    {
                        var match = Regex.Match(source, pattern);
                        violatingFiles.Add($"{relativePath} (found: {match.Value.Trim()})");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-E1: Telemetry.Record() calls must only appear in *TelemetryMiddleware*.cs files or *PipelineFactory*.cs files " +
                "(where the call is part of middleware lambda setup). " +
                "Direct telemetry recording outside middleware breaks the pipeline pattern — " +
                "telemetry should be captured as a cross-cutting concern within the middleware chain. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "E")]
        public void R_E1_Telemetry_Record_KnownViolations_Should_Not_Grow()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var actualViolations = new List<string>();

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "backup" + Path.DirectorySeparatorChar)))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (AllowedFiles.Contains(relativePath))
                    continue;

                var source = File.ReadAllText(file);

                foreach (var pattern in TelemetryRecordPatterns)
                {
                    if (Regex.IsMatch(source, pattern))
                    {
                        actualViolations.Add(relativePath);
                        break;
                    }
                }
            }

            var newViolations = actualViolations
                .Where(v => !KnownViolations.Contains(v) && !KnownViolations.Contains(Path.GetFileName(v)))
                .ToList();

            newViolations.Should().BeEmpty(
                "R-E1: New Telemetry.Record() violations detected beyond the known whitelist. " +
                "Known violations (to be refactored): " + string.Join(", ", KnownViolations) + ". " +
                "New violations must either be moved into a TelemetryMiddleware or added to the whitelist with justification. " +
                $"New violations:\n  {string.Join("\n  ", newViolations)}");
        }

        [Fact]
        [Trait("Phase", "E")]
        public void R_E1_TelemetryMiddleware_Files_Should_Exist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var expectedMiddlewareFiles = new[]
            {
                Path.Combine(sourceDir, "Application", "Features", "Pipeline", "Bus", "BusPublishTelemetryMiddleware.cs"),
            };

            foreach (var expected in expectedMiddlewareFiles)
            {
                File.Exists(expected).Should().BeTrue(
                    $"R-E1: Expected telemetry middleware file must exist at {expected}. " +
                    "Without dedicated middleware, telemetry recording cannot be enforced as a pipeline concern.");
            }
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(TelemetryOnlyInMiddlewareTests).Assembly.Location);
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
