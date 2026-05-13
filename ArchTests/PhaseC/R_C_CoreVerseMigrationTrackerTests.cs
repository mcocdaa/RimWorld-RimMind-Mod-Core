using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseC
{
    public class CoreVerseMigrationTrackerTests
    {
        private static readonly string[] KnownVerseUserFiles = new[]
        {
            @"Presentation\Runtime\RimMindRuntime.cs",
            @"Presentation\Runtime\RimMindRuntimeGameComponent.cs",
            @"Presentation\UI\NullAudioPlayer.cs",
            @"Presentation\Sensor\SensorManager.cs",
            @"Presentation\Sensor\ISensorProvider.cs",
            @"Presentation\Perception\PerceptionBridge.cs",
            @"Presentation\Internal\IProviderRegistry.cs",
            @"Presentation\Internal\ClientManager.cs",
            @"Presentation\Internal\ProviderRegistry.cs",
            @"Presentation\Extensions\IAgentActionBridge.cs",
            @"Presentation\Agent\ThinkNode_RimMindAgent.cs",
            @"Presentation\Agent\StrategyOptimizer.cs",
            @"Presentation\Agent\RimMindActionDef.cs",
            @"Presentation\Agent\PerceptionBuffer.cs",
            @"Presentation\Agent\PawnThinker.cs",
            @"Presentation\Agent\IPawnAgent.cs",
            @"Presentation\Agent\BehaviorRecord.cs",
            @"Presentation\Agent\AgentIdentity.cs",
            @"Presentation\Agent\AgentGoalStack.cs",
            @"Presentation\Agent\AgentGoal.cs",
            @"Presentation\Agent\PawnRecorder.cs",
            @"Presentation\Agent\PawnPerceiver.cs",
            @"Presentation\Agent\PawnAgent.cs",
            @"Presentation\Agent\GoalGenerator.cs",
            @"Presentation\Agent\NpcProfileBuilder.cs",
            @"Presentation\Agent\PawnActor.cs",
            @"Presentation\Agent\JobDriver_RimMindAction.cs",
            @"Presentation\AIDebugLog.cs",
            @"Presentation\GameContextBuilder.cs",
            @"Presentation\PawnDataExtractor.cs",
            @"Npc\NpcManager.cs",
            @"Npc\StorageDriverFactory.cs",
            @"Npc\ResponseDispatcher.cs",
            @"Npc\NpcTypes.cs",
            @"Npc\NpcProfileExposure.cs",
            @"Npc\LocalStorageDriver.cs",
            @"Npc\INpcManager.cs",
            @"Npc\Player2StorageDriver.cs",
            @"Presentation\Pipeline\Npc\NpcAliveCheckMiddleware.cs",
            @"Comps\CompPawnAgent.cs",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C_Tracker_CoreVerseDependency_Count_ShouldNotIncrease()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var coreDir = Path.Combine(sourceDir, "Presentation");
            if (!Directory.Exists(coreDir)) return;

            var verseUsingPattern = @"using\s+Verse\s*;";
            var currentVerseUsers = new List<string>();

            foreach (var file in Directory.GetFiles(coreDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (Regex.IsMatch(source, verseUsingPattern))
                {
                    var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    currentVerseUsers.Add(relativePath);
                }
            }

            var npcDir = Path.Combine(sourceDir, "Npc");
            if (Directory.Exists(npcDir))
            {
                foreach (var file in Directory.GetFiles(npcDir, "*.cs", SearchOption.AllDirectories))
                {
                    var source = File.ReadAllText(file);
                    if (Regex.IsMatch(source, verseUsingPattern))
                    {
                        var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        currentVerseUsers.Add(relativePath);
                    }
                }
            }

            var compsDir = Path.Combine(sourceDir, "Comps");
            if (Directory.Exists(compsDir))
            {
                foreach (var file in Directory.GetFiles(compsDir, "*.cs", SearchOption.AllDirectories))
                {
                    var source = File.ReadAllText(file);
                    if (Regex.IsMatch(source, verseUsingPattern))
                    {
                        var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        currentVerseUsers.Add(relativePath);
                    }
                }
            }

            currentVerseUsers.Count.Should().BeLessOrEqualTo(KnownVerseUserFiles.Length,
                "R-C-Tracker: Presentation/Npc/Comps Verse dependency count should not increase beyond known baseline. " +
                $"Known: {KnownVerseUserFiles.Length}, Current: {currentVerseUsers.Count}. " +
                "New files using 'using Verse;' in Presentation/ must be migrated to Infrastructure/ or added to KnownVerseUserFiles. " +
                $"Current Verse users:\n  {string.Join("\n  ", currentVerseUsers)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C_Tracker_AdaptersVerse_ShouldContain_AllGameComponents()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var adaptersVerseDir = Path.Combine(sourceDir, "Infrastructure", "Verse");
            if (!Directory.Exists(adaptersVerseDir)) return;

            var gameComponentFiles = Directory.GetFiles(adaptersVerseDir, "*GameComponent*.cs");
            gameComponentFiles.Length.Should().BeGreaterOrEqualTo(3,
                "R-C-Tracker: Infrastructure/Verse should contain at least 3 GameComponent files " +
                "(AIRequestQueueGameComponent, AgentBusGameComponent, FlywheelGameComponent). " +
                $"Current count: {gameComponentFiles.Length}");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(CoreVerseMigrationTrackerTests).Assembly.Location);
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
