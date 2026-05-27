using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L2_NoCurrentScenarioStatic
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

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
        public void No_ContextKeyRegistry_CurrentScenario_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("ContextKeyRegistry.CurrentScenario"),
                    $"File {file} still references ContextKeyRegistry.CurrentScenario");
            }
        }

        [Fact]
        public void No_RimMindAPI_Context_CurrentScenario_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("RimMindAPI.Context.CurrentScenario"),
                    $"File {file} still references RimMindAPI.Context.CurrentScenario");
            }
        }

        [Fact]
        public void No_RimMindAPI_Context_CurrentSpeakerName_Or_IsMonologue_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("RimMindAPI.Context.CurrentSpeakerName"),
                    $"File {file} still references RimMindAPI.Context.CurrentSpeakerName");
                Assert.False(content.Contains("RimMindAPI.Context.CurrentIsMonologue"),
                    $"File {file} still references RimMindAPI.Context.CurrentIsMonologue");
            }
        }
    }
}
