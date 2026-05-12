using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PhaseH2ArchTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(TestContext.BasePath, "..", "..", "..", "..", ".."));

        private static readonly string ActionsSourceDir = Path.Combine(RepoRoot, "RimMind-Actions", "Source");
        private static readonly string MechanismsDir = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Kernel", "Mechanisms");

        private static IEnumerable<string> GetProductionCsFiles(string root)
        {
            return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") && !f.Contains("\\Tests\\") && !f.Contains("\\Refs\\"));
        }

        [Fact]
        public void R_H6_MechanismWithLargeEnum_MustRegisterListHandler()
        {
            var largeEnumMechanisms = new Dictionary<string, string>
            {
                ["pawn.thought"] = "ThoughtDef",
                ["pawn.inspiration"] = "InspirationDef",
                ["pawn.mental_state"] = "MentalStateDef",
                ["world.storyteller"] = "IncidentDef",
            };

            var runtimeFile = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Core", "Runtime", "RimMindRuntime.cs");
            Assert.True(File.Exists(runtimeFile), "RimMindRuntime.cs must exist");
            var runtimeContent = File.ReadAllText(runtimeFile);

            foreach (var kvp in largeEnumMechanisms)
            {
                var mechanismId = kvp.Key;
                var defType = kvp.Value;

                var mechanismFile = Directory.GetFiles(MechanismsDir, "*Mechanism.cs", SearchOption.AllDirectories)
                    .FirstOrDefault(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") && !f.Contains("GameMechanismBase") && File.ReadAllText(f).Contains($"MechanismId => \"{mechanismId}\""));

                if (mechanismFile != null)
                {
                    var content = File.ReadAllText(mechanismFile);
                    var supportsList = content.Contains("MechanismOperationType.List");
                    Assert.True(supportsList,
                        $"Mechanism '{mechanismId}' uses large enum {defType} and must support List operation");
                }
            }
        }

        [Fact]
        public void R_H6_ListMechanism_MustHaveMechanismListToolHandler()
        {
            var listHandlerFile = Path.Combine(MechanismsDir, "MechanismListToolHandler.cs");
            Assert.True(File.Exists(listHandlerFile),
                "MechanismListToolHandler.cs must exist for large enum list operations");

            var content = File.ReadAllText(listHandlerFile);
            Assert.Contains("IToolHandler", content);
            Assert.Contains("category", content);
            Assert.Contains("ExecuteListAsync", content);
        }

        [Fact]
        public void R_H6_Registry_RegistersListHandlerForListCapableMechanisms()
        {
            var registryFile = Path.Combine(MechanismsDir, "GameMechanismRegistry.cs");
            Assert.True(File.Exists(registryFile), "GameMechanismRegistry.cs must exist");

            var content = File.ReadAllText(registryFile);
            Assert.Contains("MechanismListToolHandler", content);
            Assert.Contains("MechanismOperationType.List", content);
        }

        [Fact]
        public void R_H7_IActionRule_MustNotExist()
        {
            foreach (var file in GetProductionCsFiles(RepoRoot))
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("IActionRule", content);
            }
        }

        [Fact]
        public void R_H7_PawnActions_MustNotExist()
        {
            var deletedActionFiles = new[]
            {
                "PawnActions.cs", "MoodActions.cs", "SocialActions.cs",
                "RelationActions.cs", "EventActions.cs"
            };

            var actionsDir = Path.Combine(ActionsSourceDir, "Actions");
            if (Directory.Exists(actionsDir))
            {
                var files = Directory.GetFiles(actionsDir, "*.cs");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    Assert.False(deletedActionFiles.Contains(fileName),
                        $"Deleted action file should not exist: {fileName}");
                }
            }
        }

        [Fact]
        public void R_H7_ActionResult_MustOnlyExistAsObsoleteShim()
        {
            var apiFile = Path.Combine(ActionsSourceDir, "RimMindActionsAPI.cs");
            if (!File.Exists(apiFile)) return;

            var content = File.ReadAllText(apiFile);
            if (content.Contains("class ActionResult"))
            {
                Assert.True(content.Contains("[Obsolete") || content.Contains("[ObsoleteAttribute"),
                    "ActionResult class must be marked [Obsolete] if it exists as a compatibility shim");
            }

            foreach (var file in GetProductionCsFiles(RepoRoot))
            {
                if (file == apiFile) continue;
                var fcontent = File.ReadAllText(file);
                Assert.DoesNotContain("class ActionResult", fcontent);
            }
        }

        [Fact]
        public void R_H7_RiskLevel_MustNotExistInMechanismLayer()
        {
            var mechanismFiles = Directory.GetFiles(MechanismsDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                .ToList();

            foreach (var file in mechanismFiles)
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("RiskLevel", content);
            }
        }

        [Fact]
        public void R_H8_RimMindActionsAPI_PublicMethods_MustBeObsolete()
        {
            var apiFile = Directory.GetFiles(ActionsSourceDir, "RimMindActionsAPI.cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            Assert.NotNull(apiFile);

            var content = File.ReadAllText(apiFile);

            var publicMethods = new[] { "RegisterAction", "Execute", "GetStructuredTools" };

            foreach (var method in publicMethods)
            {
                Assert.True(content.Contains("[Obsolete") || content.Contains("[ObsoleteAttribute"),
                    $"RimMindActionsAPI.{method} must be marked [Obsolete]");
            }
        }

        [Fact]
        public void R_H8_ActionsBridge_MustBeEmptyShell()
        {
            var bridgeFile = Directory.GetFiles(ActionsSourceDir, "ActionsBridge.cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            Assert.NotNull(bridgeFile);

            var content = File.ReadAllText(bridgeFile);

            Assert.DoesNotContain("IActionRule", content);
            Assert.True(content.Contains("[Obsolete") || content.Contains("no-op") || content.Contains("No-op"),
                "ActionsBridge should be an empty shell with [Obsolete] or no-op markers");
        }

        [Fact]
        public void H2_All17MechanismFiles_MustExist()
        {
            var expectedMechanisms = new[]
            {
                "Pawn\\Job\\JobMechanism.cs",
                "Pawn\\Draft\\DraftMechanism.cs",
                "Pawn\\Work\\WorkMechanism.cs",
                "Pawn\\Equipment\\EquipmentMechanism.cs",
                "Pawn\\Interaction\\InteractionMechanism.cs",
                "Pawn\\Recruit\\RecruitMechanism.cs",
                "Pawn\\Thought\\ThoughtMechanism.cs",
                "Pawn\\Inspiration\\InspirationMechanism.cs",
                "Pawn\\MentalState\\MentalStateMechanism.cs",
                "Pawn\\Health\\HealthMechanism.cs",
                "Pawn\\Relations\\RelationsMechanism.cs",
                "Pawn\\Skill\\SkillMechanism.cs",
                "Pawn\\Need\\NeedMechanism.cs",
                "Map\\Wealth\\WealthMechanism.cs",
                "World\\Faction\\FactionMechanism.cs",
                "World\\Storyteller\\StorytellerMechanism.cs",
                "World\\ChoiceLetter\\ChoiceLetterMechanism.cs",
            };

            foreach (var relativePath in expectedMechanisms)
            {
                var fullPath = Path.Combine(MechanismsDir, relativePath);
                Assert.True(File.Exists(fullPath),
                    $"Mechanism file must exist: {relativePath}");
            }
        }

        [Fact]
        public void H2_MechanismId_MustFollowTwoSegmentPattern()
        {
            var mechanismFiles = Directory.GetFiles(MechanismsDir, "*Mechanism.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") && !f.Contains("GameMechanismBase"))
                .ToList();

            foreach (var file in mechanismFiles)
            {
                var content = File.ReadAllText(file);
                var mechanismIdMatch = System.Text.RegularExpressions.Regex.Match(
                    content, @"MechanismId\s*=>\s*""([^""]+)""");

                if (mechanismIdMatch.Success)
                {
                    var mechanismId = mechanismIdMatch.Groups[1].Value;
                    var segments = mechanismId.Split('.');
                    Assert.True(segments.Length == 2,
                        $"MechanismId '{mechanismId}' in {Path.GetFileName(file)} should be two segments, got {segments.Length}");
                }
            }
        }

        [Fact]
        public void H2_ActionsModule_OnlyContainsShellFiles()
        {
            if (!Directory.Exists(ActionsSourceDir)) return;

            var csFiles = Directory.GetFiles(ActionsSourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                .Select(f => Path.GetFileName(f))
                .ToList();

            var allowedFiles = new HashSet<string>
            {
                "RimMindActionsAPI.cs",
                "RimMindActionsMod.cs",
                "ActionsBridge.cs",
                "RimMindActionsSettings.cs"
            };

            foreach (var file in csFiles)
            {
                Assert.True(allowedFiles.Contains(file),
                    $"Unexpected file in Actions module: {file}. Only shell files should remain.");
            }
        }

        [Fact]
        public void H2_RegisterAllMechanisms_InRuntime()
        {
            var runtimeFile = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Core", "Runtime", "RimMindRuntime.cs");
            Assert.True(File.Exists(runtimeFile), "RimMindRuntime.cs must exist");

            var content = File.ReadAllText(runtimeFile);

            var requiredMechanisms = new[]
            {
                "JobMechanism", "DraftMechanism", "WorkMechanism", "EquipmentMechanism",
                "InteractionMechanism", "RecruitMechanism", "ThoughtMechanism",
                "InspirationMechanism", "MentalStateMechanism", "HealthMechanism",
                "RelationsMechanism", "SkillMechanism", "NeedMechanism",
                "WealthMechanism", "FactionMechanism", "StorytellerMechanism",
                "ChoiceLetterMechanism"
            };

            foreach (var mechanism in requiredMechanisms)
            {
                Assert.True(content.Contains(mechanism),
                    $"RimMindRuntime must register {mechanism}");
            }
        }
    }

    internal static class TestContext
    {
        public static string BasePath => System.AppContext.BaseDirectory;
    }
}
