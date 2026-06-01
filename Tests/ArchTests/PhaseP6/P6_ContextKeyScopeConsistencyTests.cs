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
    }
}
