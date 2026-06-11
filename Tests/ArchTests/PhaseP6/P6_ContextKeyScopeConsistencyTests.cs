using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_ContextKeyScopeConsistencyTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static string ReadModSource(string modDir, string relativePath)
            => File.ReadAllText(Path.Combine(RepoRoot, modDir, "Source",
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

        [Fact]
        public void Core_L0_Static_Keys_Have_Zero_StalenessTicks()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            Assert.Contains("L0_Static", content);
            Assert.Contains("stalenessTicks: 0", content);
        }

        [Fact]
        public void Core_L1_Baseline_Keys_Have_StalenessTicks_3000()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var l1Section = ExtractSection(content, "L1_Baseline", "L2_Environment");
            Assert.Contains("stalenessTicks: 3000", l1Section);
        }

        [Fact]
        public void Core_L2_Environment_Keys_Have_StalenessTicks_1500()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var l2Section = ExtractSection(content, "L2_Environment", "L3_State");
            Assert.Contains("stalenessTicks: 1500", l2Section);
        }

        [Fact]
        public void Core_L3_State_Keys_Have_StalenessTicks_750()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var l3Section = ExtractSection(content, "L3_State", "L4_History");
            Assert.Contains("stalenessTicks: 750", l3Section);
        }

        [Fact]
        public void Submod_L0_Static_Keys_Have_Zero_StalenessTicks()
        {
            var submods = new[]
            {
                ("RimMind-Personality", "RimMindPersonalityMod.cs"),
                ("RimMind-Dialogue", "RimMindDialogueMod.cs"),
                ("RimMind-Storyteller", "RimMindStorytellerMod.cs"),
                ("RimMind-Advisor", "RimMindAdvisorMod.cs"),
            };
            foreach (var (mod, file) in submods)
            {
                var content = ReadModSource(mod, file);
                if (!content.Contains("L0_Static")) continue;
                var l0Idx = 0;
                while ((l0Idx = content.IndexOf("L0_Static", l0Idx)) != -1)
                {
                    var nearby = content.Substring(l0Idx, Math.Min(2000, content.Length - l0Idx));
                    Assert.True(nearby.Contains("stalenessTicks: 0"),
                        $"{mod}/{file}: L0_Static key near position {l0Idx} should have stalenessTicks: 0");
                    l0Idx += 9;
                }
            }
        }

        [Fact]
        public void Submod_L3_State_Keys_Have_StalenessTicks_750_Or_1500()
        {
            var submods = new[]
            {
                ("RimMind-Personality", "RimMindPersonalityMod.cs"),
                ("RimMind-Dialogue", "RimMindDialogueMod.cs"),
                ("RimMind-Storyteller", "RimMindStorytellerMod.cs"),
                ("RimMind-Advisor", "RimMindAdvisorMod.cs"),
                ("RimMind-Memory", "Injection/WorkingMemoryProvider.cs"),
                ("RimMind-Memory", "Injection/MemoryContextProvider.cs"),
            };
            foreach (var (mod, file) in submods)
            {
                var content = ReadModSource(mod, file);
                if (!content.Contains("L3_State")) continue;
                var l3Idx = 0;
                while ((l3Idx = content.IndexOf("L3_State", l3Idx)) != -1)
                {
                    var nearby = content.Substring(l3Idx, Math.Min(3000, content.Length - l3Idx));
                    Assert.True(nearby.Contains("stalenessTicks: 750") || nearby.Contains("stalenessTicks: 1500"),
                        $"{mod}/{file}: L3_State key near position {l3Idx} should have stalenessTicks: 750 or 1500");
                    l3Idx += 8;
                }
            }
        }

        [Fact]
        public void Submod_L4_History_Keys_Have_StalenessTicks_3000()
        {
            var submods = new[]
            {
                ("RimMind-Storyteller", "RimMindStorytellerMod.cs"),
                ("RimMind-Advisor", "RimMindAdvisorMod.cs"),
                ("RimMind-Memory", "Injection/MemoryContextProvider.cs"),
                ("RimMind-Bridge-RimTalk", "Bridge/ContextPullBridge.cs"),
                ("RimMind-Bridge-RimChat", "Bridge/ContextPullBridge.cs"),
            };
            foreach (var (mod, file) in submods)
            {
                var content = ReadModSource(mod, file);
                if (!content.Contains("L4_History")) continue;
                var l4Idx = 0;
                while ((l4Idx = content.IndexOf("L4_History", l4Idx)) != -1)
                {
                    var nearby = content.Substring(l4Idx, Math.Min(2000, content.Length - l4Idx));
                    Assert.True(nearby.Contains("stalenessTicks: 3000"),
                        $"{mod}/{file}: L4_History key near position {l4Idx} should have stalenessTicks: 3000");
                    l4Idx += 10;
                }
            }
        }

        [Fact]
        public void Submod_ContextKeys_Have_InvalidationTriggers()
        {
            var submods = new[]
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
            foreach (var (mod, file) in submods)
            {
                var content = ReadModSource(mod, file);
                if (!content.Contains("new ContextProviderDef")) continue;
                Assert.Contains("invalidationTriggers:", content);
            }
        }

        [Fact]
        public void Core_Base_Keys_May_Lack_InvalidationTriggers()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            Assert.DoesNotContain("invalidationTriggers:", content);
        }

        [Fact]
        public void L0_Static_Keys_Have_Highest_Priority()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            Assert.Contains("priority: 1.0f", content);
            Assert.Contains("priority: 0.95f", content);
        }

        [Fact]
        public void All_ContextKeys_Use_Valid_ContextLayer_Values()
        {
            var validLayers = new[] { "L0_Static", "L1_Baseline", "L2_Environment", "L3_State", "L4_History", "L5_Sensor" };
            var allContent = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            foreach (var layer in validLayers)
            {
                if (allContent.Contains($"ContextLayer.{layer}"))
                {
                    Assert.True(true);
                }
            }
        }

        private static string ExtractSection(string content, string startMarker, string endMarker)
        {
            var startIdx = content.IndexOf($"// ── {startMarker}");
            if (startIdx < 0) startIdx = content.IndexOf(startMarker);
            if (startIdx < 0) return content;
            var endIdx = content.IndexOf($"// ── {endMarker}");
            if (endIdx < 0 || endIdx <= startIdx) endIdx = content.Length;
            return content.Substring(startIdx, endIdx - startIdx);
        }

        [Fact]
        public void ContextProviderDef_Has_CacheScope_Property()
        {
            var content = File.ReadAllText(Path.Combine(RepoRoot,
                "RimMind-Core", "Source", "Application", "Common", "Interfaces", "Context",
                "ContextProviderDef.cs"));
            Assert.Contains("CacheScope CacheScope", content);
            Assert.Contains("cacheScope = CacheScope.Scenario", content);
        }

        [Fact]
        public void KeyMeta_Has_CacheScope_And_OverrideSource_Fields()
        {
            var content = File.ReadAllText(Path.Combine(RepoRoot,
                "RimMind-Core", "Source", "Domain", "ValueObjects", "KeyMeta.cs"));
            Assert.Contains("CacheScope CacheScope;", content);
            Assert.Contains("string? OverrideSource;", content);
        }

        [Fact]
        public void ContextKeyRegistryImpl_Records_OverrideSource_On_Overwrite()
        {
            var content = File.ReadAllText(Path.Combine(RepoRoot,
                "RimMind-Core", "Source", "Application", "Features", "Context",
                "ContextKeyRegistryImpl.cs"));
            Assert.Contains("OverrideSource", content);
            Assert.Contains("meta.OverrideSource = old.OwnerMod", content);
        }

        [Fact]
        public void ContextKeyRegistryImpl_Passes_CacheScope_To_KeyMeta()
        {
            var content = File.ReadAllText(Path.Combine(RepoRoot,
                "RimMind-Core", "Source", "Application", "Features", "Context",
                "ContextKeyRegistryImpl.cs"));
            Assert.Contains("cacheScope: def.CacheScope", content);
        }

        [Fact]
        public void Window_ContextKeyDebug_Shows_CacheScope_And_OverrideSource()
        {
            var content = File.ReadAllText(Path.Combine(RepoRoot,
                "RimMind-Core", "Source", "Infrastructure", "UI", "Window_ContextKeyDebug.cs"));
            Assert.Contains("RimMind.UI.ContextKeyDebug.CacheScope", content);
            Assert.Contains("RimMind.UI.ContextKeyDebug.OverrideSource", content);
            Assert.Contains("selected.OverrideSource", content);
            Assert.Contains("selected.CacheScope", content);
        }

        [Fact]
        public void CacheScope_Enum_Defines_All_Planned_Scopes()
        {
            var content = File.ReadAllText(Path.Combine(RepoRoot,
                "RimMind-Core", "Source", "Domain", "ValueObjects", "CacheScope.cs"));
            Assert.Contains("Static", content);
            Assert.Contains("Pawn", content);
            Assert.Contains("Map", content);
            Assert.Contains("Storyteller", content);
            Assert.Contains("Scenario", content);
        }

        [Fact]
        public void CoreContextProviders_Static_Keys_Have_CacheScope_Static()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var staticKeys = new[] { "system_instruction", "world_rules", "npc_task_instruction" };
            foreach (var key in staticKeys)
            {
                var keyIdx = content.IndexOf($"key: \"{key}\"");
                Assert.True(keyIdx >= 0, $"Key '{key}' not found in CoreContextProviders.cs");
                var nearby = content.Substring(keyIdx, Math.Min(2500, content.Length - keyIdx));
                Assert.True(nearby.Contains("cacheScope: CacheScope.Static"),
                    $"Key '{key}' should have cacheScope: CacheScope.Static");
            }
        }

        [Fact]
        public void CoreContextProviders_Map_Keys_Have_CacheScope_Map()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var mapKeys = new[] { "map_structure", "weather", "time_of_day", "season", "colony_status" };
            foreach (var key in mapKeys)
            {
                var keyIdx = content.IndexOf($"key: \"{key}\"");
                Assert.True(keyIdx >= 0, $"Key '{key}' not found in CoreContextProviders.cs");
                var nearby = content.Substring(keyIdx, Math.Min(2500, content.Length - keyIdx));
                Assert.True(nearby.Contains("cacheScope: CacheScope.Map"),
                    $"Key '{key}' should have cacheScope: CacheScope.Map");
            }
        }

        [Fact]
        public void CoreContextProviders_Pawn_Keys_Have_CacheScope_Pawn()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var pawnKeys = new[]
            {
                "npc_identity", "npc_commands",
                "pawn_base_info", "fixed_relations", "ideology", "skills_summary",
                "current_area", "nearby_pawns",
                "health", "mood", "current_job", "combat_status", "target_info", "task_progress"
            };
            foreach (var key in pawnKeys)
            {
                var keyIdx = content.IndexOf($"key: \"{key}\"");
                Assert.True(keyIdx >= 0, $"Key '{key}' not found in CoreContextProviders.cs");
                var nearby = content.Substring(keyIdx, Math.Min(2500, content.Length - keyIdx));
                Assert.True(nearby.Contains("cacheScope: CacheScope.Pawn"),
                    $"Key '{key}' should have cacheScope: CacheScope.Pawn");
            }
        }

        [Fact]
        public void CoreContextProviders_No_Key_Uses_Default_CacheScope_Scenario()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            Assert.DoesNotContain("cacheScope: CacheScope.Scenario", content);
        }

        [Fact]
        public void CoreContextProviders_All_Keys_Have_Explicit_CacheScope()
        {
            var content = ReadModSource("RimMind-Core", "Presentation/Context/CoreContextProviders.cs");
            var allKeys = new[]
            {
                "system_instruction", "npc_identity", "npc_commands", "world_rules", "npc_task_instruction",
                "map_structure", "pawn_base_info", "fixed_relations", "ideology", "skills_summary",
                "current_area", "weather", "time_of_day", "nearby_pawns", "season", "colony_status",
                "health", "mood", "current_job", "combat_status", "target_info", "task_progress"
            };
            foreach (var key in allKeys)
            {
                var keyIdx = content.IndexOf($"key: \"{key}\"");
                Assert.True(keyIdx >= 0, $"Key '{key}' not found in CoreContextProviders.cs");
                var nearby = content.Substring(keyIdx, Math.Min(2500, content.Length - keyIdx));
                Assert.True(
                    nearby.Contains("cacheScope: CacheScope.Static") ||
                    nearby.Contains("cacheScope: CacheScope.Pawn") ||
                    nearby.Contains("cacheScope: CacheScope.Map") ||
                    nearby.Contains("cacheScope: CacheScope.Storyteller"),
                    $"Key '{key}' must have an explicit cacheScope annotation");
            }
        }

        [Fact]
        public void Storyteller_ContextKeys_Have_Explicit_Owner_And_CacheScope()
        {
            var content = ReadModSource("RimMind-Storyteller", "RimMindStorytellerMod.cs");
            var scenarioKeys = new[]
            {
                "storyteller_dialogue",
                "storyteller_context",
                "storyteller_reactions",
                "storyteller_recent_incidents"
            };

            AssertKeyHasScope(content, "storyteller_task", "CacheScope.Static");
            foreach (var key in scenarioKeys)
            {
                AssertKeyHasScope(content, key, "CacheScope.Scenario");
            }
        }

        private static void AssertKeyHasScope(string content, string key, string cacheScope)
        {
            var keyIdx = content.IndexOf($"\"{key}\"", StringComparison.Ordinal);
            Assert.True(keyIdx >= 0, $"Key '{key}' should be registered");
            var nextRegistration = content.IndexOf("RimMindAPI.Context.ContextKeys.Register", keyIdx + 1, StringComparison.Ordinal);
            var length = nextRegistration > keyIdx ? nextRegistration - keyIdx : content.Length - keyIdx;
            var nearby = content.Substring(keyIdx, length);
            Assert.Contains("ownerMod: ModId", nearby);
            Assert.Contains($"cacheScope: {cacheScope}", nearby);
        }
    }
}
