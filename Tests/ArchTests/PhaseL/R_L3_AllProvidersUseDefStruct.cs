using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L3_AllProvidersUseDefStruct
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
        public void No_Old_Style_ContextKeyRegistry_Register_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("ContextKeyRegistry.Register("),
                    $"File {file} still uses old ContextKeyRegistry.Register() API");
            }
        }

        [Fact]
        public void No_RegisterPawnContextProvider_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("RegisterPawnContextProvider"),
                    $"File {file} still uses old RegisterPawnContextProvider API");
            }
        }

        [Fact]
        public void No_RegisterContextKey_In_Source()
        {
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                Assert.False(content.Contains("RimMindAPI.Context.RegisterContextKey"),
                    $"File {file} still uses old RimMindAPI.Context.RegisterContextKey API");
            }
        }

        [Fact]
        public void At_Least_40_ContextProviderDef_Registrations_In_Source()
        {
            int totalDefs = 0;
            foreach (var file in GetProductionCsFiles())
            {
                var content = File.ReadAllText(file);
                totalDefs += CountOccurrences(content, "new ContextProviderDef");
            }

            Assert.True(totalDefs >= 40,
                $"Expected at least 40 ContextProviderDef registrations, found {totalDefs}");
        }

        private static int CountOccurrences(string source, string pattern)
        {
            int count = 0;
            int idx = 0;
            while ((idx = source.IndexOf(pattern, idx)) != -1)
            {
                count++;
                idx += pattern.Length;
            }
            return count;
        }
    }
}
