using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseE
{
    public class PipelineInstanceFromRuntimeTests
    {
        private static readonly HashSet<string> PipelineFactoryFiles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            @"Application\Pipeline\AI\AIRequestPipelineFactory.cs",
            @"Application\Pipeline\Npc\NpcChatPipelineFactory.cs",
            @"Application\Pipeline\Bus\BusPublishPipelineFactory.cs",
            @"Application\Pipeline\Context\ContextBuildPipelineFactory.cs",
        };

        private static readonly HashSet<string> KnownNewPipelineViolations = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
        };

        private static readonly HashSet<string> KnownExecuteAsyncViolations = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            @"Presentation\Runtime\RimMindRuntime.cs",
            @"Application\Tools\ToolCallDispatchMiddleware.cs",
        };

        [Fact]
        [Trait("Phase", "E")]
        public void R_E3_New_Pipeline_Should_Only_Be_In_PipelineFactory()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var violatingFiles = new List<string>();
            var newPipelinePattern = @"new\s+Pipeline\s*<";

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                if (PipelineFactoryFiles.Contains(relativePath))
                    continue;

                if (KnownNewPipelineViolations.Contains(relativePath))
                    continue;

                var source = File.ReadAllText(file);

                if (Regex.IsMatch(source, newPipelinePattern))
                {
                    violatingFiles.Add($"{relativePath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-E3: 'new Pipeline<T>(...)' instantiation must only appear in *PipelineFactory*.cs files. " +
                "Pipeline instances should be created by factory methods and stored in RimMindRuntime, " +
                "not created ad-hoc in business logic. This ensures centralized pipeline configuration, " +
                "lifecycle management, and extension point registration. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "E")]
        public void R_E3_ExecuteAsync_Should_Use_Runtime_Pipeline_Instance()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var violatingFiles = new List<string>();
            var executeAsyncPattern = @"\.ExecuteAsync\s*\(";

            foreach (var file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                if (relativePath.StartsWith("Application" + Path.DirectorySeparatorChar + "Pipeline"))
                    continue;

                if (relativePath.StartsWith("Domain" + Path.DirectorySeparatorChar))
                    continue;

                if (relativePath.Contains("PipelineFactory"))
                    continue;

                if (KnownExecuteAsyncViolations.Contains(relativePath))
                    continue;

                var source = File.ReadAllText(file);

                if (!Regex.IsMatch(source, executeAsyncPattern))
                    continue;

                var lines = source.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (Regex.IsMatch(line, executeAsyncPattern))
                    {
                        var hasRuntimePrefix = Regex.IsMatch(line, @"RimMindRuntime\.Instance\.\w+Pipeline\.ExecuteAsync")
                            || Regex.IsMatch(line, @"RimMindRuntime\.Instance\.\w+Pipeline\s*\)");

                        if (!hasRuntimePrefix && !line.TrimStart().StartsWith("//"))
                        {
                            violatingFiles.Add($"{relativePath}:{i + 1} (line: {line.Trim()})");
                            break;
                        }
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-E3: Pipeline.ExecuteAsync() calls must use pipeline instances from RimMindRuntime.Instance. " +
                "Ad-hoc pipeline execution bypasses the centralized lifecycle, extension registration, " +
                "and middleware chain configured at runtime initialization. " +
                "Pattern: RimMindRuntime.Instance.*Pipeline.ExecuteAsync(ctx). " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "E")]
        public void R_E3_Runtime_Should_Expose_Pipeline_Properties()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var runtimeFile = Directory.GetFiles(sourceDir, "RimMindRuntime.cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            runtimeFile.Should().NotBeNull("RimMindRuntime.cs must exist in the source tree");

            if (runtimeFile != null)
            {
                var source = File.ReadAllText(runtimeFile);

                source.Should().Contain("AIRequestPipeline",
                    "R-E3: RimMindRuntime must expose AIRequestPipeline property for centralized pipeline access");
                source.Should().Contain("NpcChatPipeline",
                    "R-E3: RimMindRuntime must expose NpcChatPipeline property for centralized pipeline access");
            }
        }

        [Fact]
        [Trait("Phase", "E")]
        public void R_E3_PipelineFactory_Files_Should_Exist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            foreach (var factoryRelativePath in PipelineFactoryFiles)
            {
                var fullPath = Path.Combine(sourceDir, factoryRelativePath);
                File.Exists(fullPath).Should().BeTrue(
                    $"R-E3: Expected pipeline factory file must exist at {factoryRelativePath}. " +
                    "Pipeline factories are the only authorized location for 'new Pipeline<T>' instantiation.");
            }
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(PipelineInstanceFromRuntimeTests).Assembly.Location);
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
