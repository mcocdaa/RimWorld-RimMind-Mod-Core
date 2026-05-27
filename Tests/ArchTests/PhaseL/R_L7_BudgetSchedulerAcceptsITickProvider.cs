using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L7_BudgetSchedulerAcceptsITickProvider
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string BudgetSchedulerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "BudgetScheduler.cs");

        [Fact]
        public void BudgetScheduler_Constructor_Accepts_ITickProvider()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("ITickProvider", content);
            Assert.Contains("tickProvider", content);
        }

        [Fact]
        public void Schedule_Uses_TickProvider_For_NowTicks()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("_tickProvider", content);
            Assert.Contains("TicksGame", content);
            Assert.DoesNotContain("nowTicks = 0;", content);
        }

        [Fact]
        public void BudgetScheduler_Constructor_Accepts_IEmbedCache()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("IEmbedCache", content);
            Assert.Contains("embedCache", content);
        }

        [Fact]
        public void ComputeQuerySimilarity_Uses_EmbedCache()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("CosineSimilarity", content);
            Assert.Contains("_embedCache", content);
        }
    }
}
