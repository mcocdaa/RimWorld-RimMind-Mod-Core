using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseE
{
    public class TransientCheckerInMiddlewareTests
    {
        private static readonly string[] TransientCheckerPatterns = new[]
        {
            @"TransientExceptionChecker\.IsTransient",
        };

        private static readonly HashSet<string> AllowedFiles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            @"Application\Features\Pipeline\AI\RetryMiddleware.cs",
            @"Application\Common\Behaviours\CommonRetryMiddleware.cs",
            @"Application\Features\Pipeline\Npc\NpcChatRetryMiddleware.cs",
            @"Presentation\Pipeline\Npc\NpcChatPipelineFactory.cs",
            @"Presentation\Pipeline\AI\AIRequestPipelineFactory.cs",
            @"Presentation\Pipeline\Context\ContextBuildPipelineFactory.cs",
            @"Application\Features\Pipeline\Bus\BusPublishPipelineFactory.cs",
        };

        private static readonly HashSet<string> KnownViolations = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
        };

        [Fact]
        [Trait("Phase", "E")]
        public void R_E2_TransientExceptionChecker_Should_Only_Be_Called_In_RetryMiddleware()
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

                var source = File.ReadAllText(file);

                foreach (var pattern in TransientCheckerPatterns)
                {
                    if (Regex.IsMatch(source, pattern))
                    {
                        violatingFiles.Add($"{relativePath}");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-E2: TransientExceptionChecker.IsTransient() calls must only appear in *RetryMiddleware*.cs files " +
                "or *PipelineFactory*.cs files (where the call is part of middleware lambda setup). " +
                "Transient exception checking is a retry concern — it should be encapsulated within the retry middleware layer, " +
                "not scattered across business logic. Use the middleware pipeline for retry/fallback behavior. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "E")]
        public void R_E2_TransientExceptionChecker_KnownViolations_Should_Not_Grow()
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

                foreach (var pattern in TransientCheckerPatterns)
                {
                    if (Regex.IsMatch(source, pattern))
                    {
                        actualViolations.Add(relativePath);
                        break;
                    }
                }
            }

            var newViolations = actualViolations
                .Where(v => !KnownViolations.Contains(v))
                .ToList();

            newViolations.Should().BeEmpty(
                "R-E2: New TransientExceptionChecker violations detected beyond the known whitelist. " +
                "Known violations (to be refactored): " + string.Join(", ", KnownViolations) + ". " +
                "New violations must either be moved into a RetryMiddleware or added to the whitelist with justification. " +
                $"New violations:\n  {string.Join("\n  ", newViolations)}");
        }

        [Fact]
        [Trait("Phase", "E")]
        public void R_E2_RetryMiddleware_Files_Should_Exist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var expectedMiddlewareFiles = new[]
            {
                Path.Combine(sourceDir, "Application", "Features", "Pipeline", "AI", "RetryMiddleware.cs"),
                Path.Combine(sourceDir, "Application", "Common", "Behaviours", "CommonRetryMiddleware.cs"),
            };

            foreach (var expected in expectedMiddlewareFiles)
            {
                File.Exists(expected).Should().BeTrue(
                    $"R-E2: Expected retry middleware file must exist at {expected}. " +
                    "Without dedicated retry middleware, transient exception checking cannot be enforced as a pipeline concern.");
            }
        }

        private static readonly string KnownDefinitionLocation = @"Presentation\Runtime\TransientExceptionChecker.cs";

        [Fact]
        [Trait("Phase", "E")]
        public void R_E2_TransientExceptionChecker_Definition_Location_Should_Not_Regress()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var definitionFile = Directory.GetFiles(sourceDir, "TransientExceptionChecker.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains(Path.DirectorySeparatorChar + "backup" + Path.DirectorySeparatorChar));

            definitionFile.Should().NotBeNull(
                "R-E2: TransientExceptionChecker.cs definition file must exist in the source tree");

            if (definitionFile != null)
            {
                var relativePath = definitionFile.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var isInApplicationOrDomain = relativePath.StartsWith("Application") || relativePath.StartsWith("Domain");

                if (!isInApplicationOrDomain)
                {
                    relativePath.Should().Be(KnownDefinitionLocation,
                        $"R-E2: TransientExceptionChecker is currently at {relativePath} (known debt: should be in Application/ or Domain/). " +
                        "If it has been moved to a new location, update KnownDefinitionLocation. " +
                        "If it has been moved to Application/ or Domain/, this test will auto-pass.");
                }
            }
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(TransientCheckerInMiddlewareTests).Assembly.Location);
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
