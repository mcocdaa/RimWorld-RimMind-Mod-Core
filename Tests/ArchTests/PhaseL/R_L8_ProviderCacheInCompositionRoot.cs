using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L8_ProviderCacheInCompositionRoot
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string ContextCompositionPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Runtime", "Composition", "ContextComposition.cs");

        [Fact]
        public void CompositionRoot_Instantiates_ProviderCache()
        {
            Assert.True(File.Exists(ContextCompositionPath), "ContextComposition.cs must exist");

            var content = File.ReadAllText(ContextCompositionPath);

            Assert.Contains("new ProviderCache", content);
        }

        [Fact]
        public void CompositionRoot_Passes_ProviderCache_To_ContextOrchestrator()
        {
            Assert.True(File.Exists(ContextCompositionPath), "ContextComposition.cs must exist");

            var content = File.ReadAllText(ContextCompositionPath);

            Assert.Contains("providerCache", content);
            Assert.Contains("ContextOrchestrator", content);
        }

        [Fact]
        public void CompositionRoot_Passes_ITickProvider_To_BudgetScheduler()
        {
            Assert.True(File.Exists(ContextCompositionPath), "ContextComposition.cs must exist");

            var content = File.ReadAllText(ContextCompositionPath);

            Assert.Contains("BudgetScheduler", content);
            Assert.Contains("tickProvider", content);
        }

        [Fact]
        public void CompositionRoot_Passes_IEmbedCache_To_BudgetScheduler()
        {
            Assert.True(File.Exists(ContextCompositionPath), "ContextComposition.cs must exist");

            var content = File.ReadAllText(ContextCompositionPath);

            Assert.Contains("EmbedCache", content);
        }
    }
}
