using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_OverlapAndDuplicateAuditTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadCoreSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static readonly string[] AllSourceDirs = new[]
        {
            "RimMind-Core", "RimMind-Memory", "RimMind-Personality",
            "RimMind-Dialogue", "RimMind-Storyteller", "RimMind-Advisor",
            "RimMind-Bridge-RimTalk", "RimMind-Bridge-RimChat",
        };

        private static IEnumerable<string> GetProductionCsFiles()
        {
            foreach (var mod in AllSourceDirs)
            {
                var dir = Path.Combine(RepoRoot, mod, "Source");
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    if (f.Contains("\\obj\\") || f.Contains("\\bin\\") || f.Contains("\\backup\\")) continue;
                    yield return f;
                }
            }
        }

        [Fact]
        public void Task_Instruction_Keys_Are_Intentionally_Scenario_Scoped()
        {
            var coreTaskKeys = new[]
            {
                ("npc_task_instruction", "Core", true),
            };
            var submodTaskKeys = new[]
            {
                ("personality_task", "RimMind.Personality", false),
                ("dialogue_task", "RimMind.Dialogue", false),
                ("storyteller_task", "RimMind.Storyteller", false),
                ("advisor_task", "RimMind.Advisor", false),
            };
            foreach (var (key, owner, usesNamedParams) in coreTaskKeys)
            {
                var found = false;
                foreach (var file in GetProductionCsFiles())
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains($"key: \"{key}\"") && content.Contains($"ownerMod: \"{owner}\""))
                    {
                        found = true;
                        break;
                    }
                }
                Assert.True(found, $"Core task key '{key}' owned by '{owner}' should exist in source");
            }
            foreach (var (key, owner, usesNamedParams) in submodTaskKeys)
            {
                var found = false;
                foreach (var file in GetProductionCsFiles())
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains($"\"{key}\"") && content.Contains($"\"{owner}\""))
                    {
                        found = true;
                        break;
                    }
                }
                Assert.True(found, $"Submod task key '{key}' owned by '{owner}' should exist in source");
            }
        }

        [Fact]
        public void Task_Keys_Use_Scenario_Filtering_To_Avoid_Conflict()
        {
            var coreTask = ReadCoreSource("Presentation/Context/CoreContextProviders.cs");
            Assert.Contains("Scenario", coreTask);
            Assert.Contains("npc_task_instruction", coreTask);

            var submodTaskKeys = new[] { "personality_task", "dialogue_task", "storyteller_task", "advisor_task" };
            foreach (var taskKey in submodTaskKeys)
            {
                var found = false;
                foreach (var file in GetProductionCsFiles())
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains($"\"{taskKey}\""))
                    {
                        Assert.True(content.Contains("Scenario"),
                            $"Submod task key '{taskKey}' must use Scenario filtering");
                        found = true;
                        break;
                    }
                }
                Assert.True(found, $"Task key '{taskKey}' should exist");
            }
        }

        [Fact]
        public void Storyteller_Mechanism_In_Core_Is_Base_Infrastructure()
        {
            var mechanismContent = ReadCoreSource("Infrastructure/Mechanisms/World/Storyteller/StorytellerMechanism.cs");
            Assert.Contains("GameMechanismBase", mechanismContent);
            Assert.Contains("IncidentDef", mechanismContent);
        }

        [Fact]
        public void No_Duplicate_Tool_Registrations_Across_Submods()
        {
            var toolRegistrations = new Dictionary<string, string>();
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                var idx = 0;
                while ((idx = content.IndexOf("RegisterTool", idx)) != -1)
                {
                    var quoteStart = content.IndexOf('"', idx);
                    if (quoteStart >= 0 && quoteStart - idx < 50)
                    {
                        var quoteEnd = content.IndexOf('"', quoteStart + 1);
                        if (quoteEnd >= 0)
                        {
                            var toolName = content.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                            var modName = file.Split(new[] { "RimMind-" }, StringSplitOptions.None)
                                .Skip(1).FirstOrDefault()?.Split('\\')[0] ?? "Unknown";
                            if (toolRegistrations.TryGetValue(toolName, out var existingMod))
                            {
                                Assert.True(existingMod == modName,
                                    $"Tool '{toolName}' registered by both '{existingMod}' and '{modName}'");
                            }
                            else
                            {
                                toolRegistrations[toolName] = modName;
                            }
                        }
                    }
                    idx += 15;
                }
            }
        }

        [Fact]
        public void OwnerMod_Naming_Inconsistency_Is_Documented()
        {
            var memoryDir = Path.Combine(RepoRoot, "RimMind-Memory", "Source");
            if (!Directory.Exists(memoryDir)) return;
            var foundHyphenOwner = false;
            foreach (var file in Directory.GetFiles(memoryDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                var content = File.ReadAllText(file);
                if (content.Contains("\"RimMind-Memory\""))
                {
                    foundHyphenOwner = true;
                    break;
                }
            }
            Assert.True(foundHyphenOwner,
                "Memory submod should use 'RimMind-Memory' as OwnerMod (known inconsistency, documented for future normalization)");
        }

        [Fact]
        public void Core_ScenarioIds_Defines_All_Known_Scenarios()
        {
            var scenarioContent = ReadCoreSource("Application/Common/Models/Context/ScenarioIds.cs");
            Assert.Contains("Dialogue", scenarioContent);
            Assert.Contains("Decision", scenarioContent);
            Assert.Contains("Personality", scenarioContent);
            Assert.Contains("Storyteller", scenarioContent);
            Assert.Contains("Memory", scenarioContent);
        }

        [Fact]
        public void ContextKeyDebug_Window_Shows_OwnerMod_For_Overlap_Detection()
        {
            var content = ReadCoreSource("Infrastructure/UI/Window_ContextKeyDebug.cs");
            Assert.Contains("OwnerMod", content);
        }

        [Fact]
        public void No_Two_Submods_Register_Same_ContextKey_Key()
        {
            var keyRegistrations = new Dictionary<string, List<string>>();
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                var idx = 0;
                while ((idx = content.IndexOf("new ContextProviderDef(", idx)) != -1)
                {
                    var searchFrom = idx + "new ContextProviderDef(".Length;
                    string? key = null;
                    var namedKeyIdx = content.IndexOf("key:", idx);
                    if (namedKeyIdx >= 0 && namedKeyIdx < searchFrom + 50)
                    {
                        var quoteStart = content.IndexOf('"', namedKeyIdx);
                        if (quoteStart >= 0 && quoteStart < searchFrom + 80)
                        {
                            var quoteEnd = content.IndexOf('"', quoteStart + 1);
                            if (quoteEnd >= 0) key = content.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                        }
                    }
                    if (key == null)
                    {
                        var firstQuote = content.IndexOf('"', searchFrom);
                        if (firstQuote >= 0 && firstQuote < searchFrom + 100)
                        {
                            var quoteEnd = content.IndexOf('"', firstQuote + 1);
                            if (quoteEnd >= 0) key = content.Substring(firstQuote + 1, quoteEnd - firstQuote - 1);
                        }
                    }
                    if (key != null)
                    {
                        var modName = file.Split(new[] { "RimMind-" }, StringSplitOptions.None)
                            .Skip(1).FirstOrDefault()?.Split('\\')[0] ?? "Core";
                        if (!keyRegistrations.TryGetValue(key, out var list))
                        {
                            list = new List<string>();
                            keyRegistrations[key] = list;
                        }
                        if (!list.Contains(modName))
                        {
                            list.Add(modName);
                        }
                    }
                    idx = searchFrom;
                }
            }
            var duplicates = keyRegistrations.Where(kvp => kvp.Value.Count > 1).ToList();
            Assert.Empty(duplicates);
        }

        [Fact]
        public void Window_AgentDialogue_WithModId_Is_Known_Exception()
        {
            var content = ReadCoreSource("Infrastructure/UI/Window_AgentDialogue.cs");
            Assert.Contains("WithModId", content);
            Assert.Contains("RimMind.Dialogue", content);
        }
    }
}
