using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L1_NoStaticContextKeyRegistry
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
        public void No_Static_ContextKeyRegistry_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("static class ContextKeyRegistry"),
                    $"File {file} still contains 'static class ContextKeyRegistry'");
            }
        }

        [Fact]
        public void No_Static_RelevanceTable_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("static class RelevanceTable"),
                    $"File {file} still contains 'static class RelevanceTable'");
            }
        }

        [Fact]
        public void No_ContextKeyRegistryAdapter_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("ContextKeyRegistryAdapter"),
                    $"File {file} still references ContextKeyRegistryAdapter");
            }
        }
    }
}
