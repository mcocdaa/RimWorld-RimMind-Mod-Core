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
            @"Core\Runtime\RimMindRuntime.cs",
            @"Core\Runtime\RimMindRuntimeGameComponent.cs",
            @"Core\UI\NullAudioPlayer.cs",
            @"Core\Sensor\SensorManager.cs",
            @"Core\Sensor\ISensorProvider.cs",
            @"Core\Sensor\ISensorManager.cs",
            @"Core\Perception\PerceptionBridge.cs",
            @"Core\Internal\IProviderRegistry.cs",
            @"Core\Internal\ClientManager.cs",
            @"Core\Internal\ProviderRegistry.cs",
            @"Core\Extensions\IAgentActionBridge.cs",
            @"Core\Agent\ThinkNode_RimMindAgent.cs",
            @"Core\Agent\StrategyOptimizer.cs",
            @"Core\Agent\RimMindActionDef.cs",
            @"Core\Agent\PerceptionBuffer.cs",
            @"Core\Agent\PawnThinker.cs",
            @"Core\Agent\IPawnAgent.cs",
            @"Core\Agent\BehaviorRecord.cs",
            @"Core\Agent\AgentIdentity.cs",
            @"Core\Agent\AgentGoalStack.cs",
            @"Core\Agent\AgentGoal.cs",
            @"Core\Agent\PawnRecorder.cs",
            @"Core\Agent\PawnPerceiver.cs",
            @"Core\Agent\PawnAgent.cs",
            @"Core\Agent\PawnActor.cs",
            @"Core\Agent\JobDriver_RimMindAction.cs",
            @"Core\AIDebugLog.cs",
            @"Core\GameContextBuilder.cs",
            @"Npc\NpcManager.cs",
            @"Npc\StorageDriverFactory.cs",
            @"Npc\ResponseDispatcher.cs",
            @"Npc\NpcTypes.cs",
            @"Npc\LocalStorageDriver.cs",
            @"Npc\INpcManager.cs",
            @"Npc\Player2StorageDriver.cs",
            @"Comps\CompPawnAgent.cs",
        };

        [Fact]
        [Trait("Phase", "C")]
        public void R_C_Tracker_CoreVerseDependency_Count_ShouldNotIncrease()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var coreDir = Path.Combine(sourceDir, "Core");
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
                "R-C-Tracker: Core/Npc/Comps Verse dependency count should not increase beyond known baseline. " +
                $"Known: {KnownVerseUserFiles.Length}, Current: {currentVerseUsers.Count}. " +
                "New files using 'using Verse;' in Core/ must be migrated to Adapters/ or added to KnownVerseUserFiles. " +
                $"Current Verse users:\n  {string.Join("\n  ", currentVerseUsers)}");
        }

        [Fact]
        [Trait("Phase", "C")]
        public void R_C_Tracker_AdaptersVerse_ShouldContain_AllGameComponents()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var adaptersVerseDir = Path.Combine(sourceDir, "Adapters", "Verse");
            if (!Directory.Exists(adaptersVerseDir)) return;

            var gameComponentFiles = Directory.GetFiles(adaptersVerseDir, "*GameComponent*.cs");
            gameComponentFiles.Length.Should().BeGreaterOrEqualTo(3,
                "R-C-Tracker: Adapters/Verse should contain at least 3 GameComponent files " +
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
