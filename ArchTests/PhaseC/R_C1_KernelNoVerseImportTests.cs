using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class ApplicationNoVerseImportTests
    {
        private static readonly string[] ForbiddenUsingPatterns = new[]
        {
            @"using\s+Verse\s*;",
            @"using\s+Verse\.AI\s*;",
            @"using\s+Verse\.Sound\s*;",
            @"using\s+Verse\.NoTest\s*;",
            @"using\s+RimWorld\s*;",
            @"using\s+RimWorld\.Planet\s*;",
        };

        private static readonly string[] AllowedFullyQualifiedTypes = new[]
        {
            "Verse.Pawn",
            "Verse.IExposable",
            "Verse.Game",
            "Verse.Map",
            "Verse.Thing",
            "Verse.Scribe_Values",
            "Verse.Scribe_Collections",
            "Verse.Scribe_Deep",
            "Verse.Scribe",
            "Verse.Scribe.mode",
            "Verse.LoadSaveMode",
            "Verse.LookMode",
            "Verse.TaggedString",
        };

        private static readonly HashSet<string> CoreCompiledFiles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            @"Pipeline\AI\ShortCircuitMiddleware.cs",
            @"Pipeline\AI\AIRequestPipelineFactory.cs",
            @"Pipeline\AI\CircuitBreakerMiddleware.cs",
            @"Pipeline\Context\BudgetTrimMiddleware.cs",
            @"Pipeline\Context\ContextBuildPipelineFactory.cs",
            @"Pipeline\Npc\NpcAliveCheckMiddleware.cs",
            @"Pipeline\Npc\StorageDriverInvokeMiddleware.cs",
            @"Pipeline\Npc\NpcChatPipelineFactory.cs",
            @"Pipeline\Npc\NpcChatShortCircuitMiddleware.cs",
            @"Llm\ResponseDispatcher.cs",
            @"Registry\ProviderRegistry.cs",
            @"Mechanisms\GameMechanismBase.cs",
            @"Mechanisms\GameMechanismBaseNoDef.cs",
            @"Mechanisms\MechanismToolHandler.cs",
            @"Mechanisms\MechanismListToolHandler.cs",
            @"Mechanisms\GameMechanismRegistry.cs",
            @"Mechanisms\Pawn\Skill\SkillMechanism.cs",
            @"Mechanisms\Pawn\Need\NeedMechanism.cs",
            @"Mechanisms\Map\Wealth\WealthMechanism.cs",
            @"Mechanisms\Pawn\Job\JobMechanism.cs",
            @"Mechanisms\Pawn\Job\JobActionHandlers.cs",
            @"Mechanisms\Pawn\Job\JobDocs.cs",
            @"Mechanisms\Pawn\Draft\DraftMechanism.cs",
            @"Mechanisms\Pawn\Work\WorkMechanism.cs",
            @"Mechanisms\Pawn\Equipment\EquipmentMechanism.cs",
            @"Mechanisms\Pawn\Interaction\InteractionMechanism.cs",
            @"Mechanisms\Pawn\Recruit\RecruitMechanism.cs",
            @"Mechanisms\Pawn\Thought\ThoughtMechanism.cs",
            @"Mechanisms\Pawn\Inspiration\InspirationMechanism.cs",
            @"Mechanisms\Pawn\MentalState\MentalStateMechanism.cs",
            @"Mechanisms\Pawn\Health\HealthMechanism.cs",
            @"Mechanisms\Pawn\Relations\RelationsMechanism.cs",
            @"Mechanisms\World\Faction\FactionMechanism.cs",
            @"Mechanisms\World\Storyteller\StorytellerMechanism.cs",
            @"Mechanisms\World\ChoiceLetter\ChoiceLetterMechanism.cs",
            @"Npc\LocalStorageDriver.cs",
            @"Npc\StorageDriverFactory.cs",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C1_Application_ShouldNot_Import_Verse_Or_RimWorld()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var applicationDir = Path.Combine(sourceDir, "Application");
            Directory.Exists(applicationDir).Should().BeTrue("Application directory must exist");
            Directory.GetFiles(applicationDir, "*.cs", SearchOption.AllDirectories).Should().NotBeEmpty(
                "Application directory must contain at least one .cs file");

            var violatingFiles = new List<string>();

            foreach (var file in Directory.GetFiles(applicationDir, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(applicationDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (CoreCompiledFiles.Contains(relativePath)) continue;
                var source = File.ReadAllText(file);

                foreach (var pattern in ForbiddenUsingPatterns)
                {
                    if (Regex.IsMatch(source, pattern, RegexOptions.Multiline))
                    {
                        var match = Regex.Match(source, pattern, RegexOptions.Multiline);
                        violatingFiles.Add($"Application/{relativePath} (found: {match.Value.Trim()})");
                        break;
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C1: Application namespace must not import Verse or RimWorld via 'using' directives. " +
                "Only Verse.Pawn and Verse.IExposable may be used via fully-qualified names. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C1_Application_FullyQualified_VerseUsage_ShouldBeLimited()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var applicationDir = Path.Combine(sourceDir, "Application");
            if (!Directory.Exists(applicationDir)) return;

            var disallowedFqUsage = new List<string>();
            var fqPattern = @"Verse\.\w+";

            foreach (var file in Directory.GetFiles(applicationDir, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(applicationDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (CoreCompiledFiles.Contains(relativePath)) continue;
                var source = File.ReadAllText(file);

                if (Regex.IsMatch(source, @"using\s+Verse")) continue;

                foreach (Match match in Regex.Matches(source, fqPattern))
                {
                    var matchedType = match.Value;
                    if (!AllowedFullyQualifiedTypes.Contains(matchedType))
                    {
                        disallowedFqUsage.Add($"Application/{relativePath} (found: {matchedType})");
                    }
                }
            }

            disallowedFqUsage.Should().BeEmpty(
                "R-C1: Application may only use fully-qualified Verse.Pawn, Verse.IExposable, Verse.Game, Verse.Map, Verse.Thing. " +
                "Other Verse types must go through Application abstractions (ILogSink, IPathProvider, etc.). " +
                $"Disallowed usages:\n  {string.Join("\n  ", disallowedFqUsage)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C1_Application_Namespace_ShouldBe_RimMind_Application()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var applicationDir = Path.Combine(sourceDir, "Application");
            if (!Directory.Exists(applicationDir)) return;

            var violatingFiles = new List<string>();
            var expectedNsPattern = @"namespace\s+RimMind\.Application";

            foreach (var file in Directory.GetFiles(applicationDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            {
                var relativePath = file.Substring(applicationDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (CoreCompiledFiles.Contains(relativePath)) continue;
                var source = File.ReadAllText(file);
                if (!Regex.IsMatch(source, expectedNsPattern))
                {
                    violatingFiles.Add($"Application/{relativePath}");
                }
            }

            violatingFiles.Should().BeEmpty(
                "R-C1: All files in Application/ directory must use RimMind.Application.* namespace. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(ApplicationNoVerseImportTests).Assembly.Location);
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
