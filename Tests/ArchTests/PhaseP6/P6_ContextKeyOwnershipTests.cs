using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_ContextKeyOwnershipTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static string ReadModSource(string modDir, string relativePath)
            => File.ReadAllText(Path.Combine(RepoRoot, modDir, "Source",
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static int CountOccurrences(string source, string pattern)
        {
            int count = 0, idx = 0;
            while ((idx = source.IndexOf(pattern, idx)) != -1) { count++; idx += pattern.Length; }
            return count;
        }

        [Fact]
        public void Core_Owns_Expected_Base_Keys()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var expectedCoreKeys = new[]
            {
                "system_instruction", "npc_identity", "npc_commands", "world_rules",
                "npc_task_instruction", "map_structure", "pawn_base_info", "fixed_relations",
                "ideology", "skills_summary", "current_area", "weather", "time_of_day",
                "nearby_pawns", "season", "colony_status", "health", "mood", "current_job",
                "combat_status", "target_info", "task_progress"
            };
            foreach (var key in expectedCoreKeys)
            {
                Assert.Contains($"key: \"{key}\"", content);
            }
            Assert.Contains("ownerMod: \"Core\"", content);
        }

        [Fact]
        public void Personality_Owns_Expected_Keys()
        {
            var content = ReadModSource("RimMind-Personality", "RimMindPersonalityMod.cs");
            Assert.Contains("\"personality_profile\"", content);
            Assert.Contains("\"personality_state\"", content);
            Assert.Contains("\"personality_shaping\"", content);
            Assert.Contains("\"personality_task\"", content);
            Assert.Contains("\"RimMind.Personality\"", content);
        }

        [Fact]
        public void Dialogue_Owns_Expected_Keys()
        {
            var content = ReadModSource("RimMind-Dialogue", "RimMindDialogueMod.cs");
            Assert.Contains("\"dialogue_state\"", content);
            Assert.Contains("\"dialogue_relation\"", content);
            Assert.Contains("\"dialogue_task\"", content);
            Assert.Contains("\"RimMind.Dialogue\"", content);
        }

        [Fact]
        public void Storyteller_Owns_Expected_Keys()
        {
            var content = ReadModSource("RimMind-Storyteller", "RimMindStorytellerMod.cs");
            Assert.Contains("\"storyteller_dialogue\"", content);
            Assert.Contains("\"storyteller_task\"", content);
            Assert.Contains("\"storyteller_context\"", content);
            Assert.Contains("\"storyteller_reactions\"", content);
            Assert.Contains("\"storyteller_recent_incidents\"", content);
            Assert.Contains("\"RimMind.Storyteller\"", content);
        }

        [Fact]
        public void Advisor_Owns_Expected_Keys()
        {
            var content = ReadModSource("RimMind-Advisor", "RimMindAdvisorMod.cs");
            Assert.Contains("\"advisor_history\"", content);
            Assert.Contains("\"actions_list\"", content);
            Assert.Contains("\"advisor_task\"", content);
            Assert.Contains("\"RimMind.Advisor\"", content);
        }

        [Fact]
        public void Memory_Owns_Expected_Keys()
        {
            var working = ReadModSource("RimMind-Memory", "Injection/WorkingMemoryProvider.cs");
            var memory = ReadModSource("RimMind-Memory", "Injection/MemoryContextProvider.cs");
            var combined = working + memory;
            Assert.Contains("\"working_memory\"", combined);
            Assert.Contains("\"memory_pawn\"", combined);
            Assert.Contains("\"memory_narrator\"", combined);
            Assert.True(
                combined.Contains("\"RimMind-Memory\"") || combined.Contains("\"RimMind.Memory\""),
                "Memory submod should declare ownerMod as string literal");
        }

        [Fact]
        public void Bridge_RimTalk_Owns_Expected_Keys()
        {
            var content = ReadModSource("RimMind-Bridge-RimTalk", "Bridge/ContextPullBridge.cs");
            Assert.Contains("\"rimtalk_history\"", content);
            Assert.True(
                content.Contains("\"RimMind.BridgeRimTalk\"") || content.Contains("ModId"),
                "RimTalk bridge should declare ownerMod via string literal or ModId constant");
        }

        [Fact]
        public void Bridge_RimChat_Owns_Expected_Keys()
        {
            var content = ReadModSource("RimMind-Bridge-RimChat", "Bridge/ContextPullBridge.cs");
            Assert.Contains("\"rimchat_diplomacy\"", content);
            Assert.Contains("\"rimchat_rpg_history\"", content);
            Assert.True(
                content.Contains("\"RimMind.BridgeRimChat\"") || content.Contains("ModId"),
                "RimChat bridge should declare ownerMod via string literal or ModId constant");
        }

        [Fact]
        public void Actions_Submod_Does_Not_Register_ContextKeys()
        {
            var actionsDir = Path.Combine(RepoRoot, "RimMind-Actions", "Source");
            if (!Directory.Exists(actionsDir)) return;
            foreach (var f in Directory.GetFiles(actionsDir, "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains("\\obj\\") || f.Contains("\\bin\\")) continue;
                var content = File.ReadAllText(f);
                Assert.DoesNotContain("new ContextProviderDef", content);
            }
        }

        [Fact]
        public void Every_Submod_ContextProviderDef_Has_OwnerMod()
        {
            var submodChecks = new (string mod, string file, string ownerLiteral)[]
            {
                ("RimMind-Personality", "RimMindPersonalityMod.cs", "\"RimMind.Personality\""),
                ("RimMind-Dialogue", "RimMindDialogueMod.cs", "\"RimMind.Dialogue\""),
                ("RimMind-Storyteller", "RimMindStorytellerMod.cs", "\"RimMind.Storyteller\""),
                ("RimMind-Advisor", "RimMindAdvisorMod.cs", "\"RimMind.Advisor\""),
                ("RimMind-Memory", "Injection/WorkingMemoryProvider.cs", "\"RimMind-Memory\""),
                ("RimMind-Memory", "Injection/MemoryContextProvider.cs", "\"RimMind-Memory\""),
                ("RimMind-Bridge-RimTalk", "Bridge/ContextPullBridge.cs", "ModId"),
                ("RimMind-Bridge-RimChat", "Bridge/ContextPullBridge.cs", "ModId"),
            };
            foreach (var (mod, file, ownerLiteral) in submodChecks)
            {
                var content = ReadModSource(mod, file);
                if (!content.Contains("new ContextProviderDef")) continue;
                Assert.Contains(ownerLiteral, content);
            }
        }

        [Fact]
        public void Core_ContextProviderDefs_Have_OwnerMod_Core()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var defCount = CountOccurrences(content, "new ContextProviderDef(");
            var ownerCount = CountOccurrences(content, "ownerMod: \"Core\"");
            Assert.True(ownerCount >= defCount,
                $"CoreContextProviders.cs: has {defCount} ContextProviderDef( but only {ownerCount} ownerMod: \"Core\" declarations");
        }

        [Fact]
        public void No_Submod_Overrides_Core_ContextKeys()
        {
            var coreKeys = new HashSet<string>
            {
                "system_instruction", "npc_identity", "npc_commands", "world_rules",
                "npc_task_instruction", "map_structure", "pawn_base_info", "fixed_relations",
                "ideology", "skills_summary", "current_area", "weather", "time_of_day",
                "nearby_pawns", "season", "colony_status", "health", "mood", "current_job",
                "combat_status", "target_info", "task_progress"
            };
            var submodFiles = new[]
            {
                ("RimMind-Personality", "RimMindPersonalityMod.cs"),
                ("RimMind-Dialogue", "RimMindDialogueMod.cs"),
                ("RimMind-Storyteller", "RimMindStorytellerMod.cs"),
                ("RimMind-Advisor", "RimMindAdvisorMod.cs"),
                ("RimMind-Memory", "Injection/WorkingMemoryProvider.cs"),
                ("RimMind-Memory", "Injection/MemoryContextProvider.cs"),
                ("RimMind-Bridge-RimTalk", "Bridge/ContextPullBridge.cs"),
                ("RimMind-Bridge-RimChat", "Bridge/ContextPullBridge.cs"),
            };
            foreach (var (mod, file) in submodFiles)
            {
                var content = ReadModSource(mod, file);
                foreach (var coreKey in coreKeys)
                {
                    Assert.DoesNotContain($"\"{coreKey}\"", content);
                }
            }
        }

        [Fact]
        public void OwnerMod_Naming_Uses_RimMind_Prefix()
        {
            var submodOwnerMods = new[]
            {
                "RimMind.Personality", "RimMind.Dialogue", "RimMind.Storyteller",
                "RimMind.Advisor", "RimMind-Memory", "RimMind.BridgeRimTalk", "RimMind.BridgeRimChat"
            };
            foreach (var owner in submodOwnerMods)
            {
                Assert.True(owner.StartsWith("RimMind"),
                    $"OwnerMod '{owner}' should start with 'RimMind'");
            }
        }

        [Fact]
        public void Total_ContextProviderDef_Count_Matches_Expected()
        {
            int total = 0;
            var allDirs = new[] { "RimMind-Core", "RimMind-Memory", "RimMind-Personality",
                "RimMind-Dialogue", "RimMind-Storyteller", "RimMind-Advisor",
                "RimMind-Bridge-RimTalk", "RimMind-Bridge-RimChat" };
            foreach (var mod in allDirs)
            {
                var dir = Path.Combine(RepoRoot, mod, "Source");
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    if (f.Contains("\\obj\\") || f.Contains("\\bin\\") || f.Contains("\\backup\\")) continue;
                    total += CountOccurrences(File.ReadAllText(f), "new ContextProviderDef(");
                }
            }
            Assert.True(total >= 43,
                $"Expected at least 43 ContextProviderDef registrations, found {total}");
        }
    }
}
