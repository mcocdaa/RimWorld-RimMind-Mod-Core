using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L4_BudgetSchedulerSevenDimensions
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string BudgetSchedulerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "BudgetScheduler.cs");

        [Fact]
        public void ScoreKey_Contains_Seven_Dimensions()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("ScoreKey", content);

            // Verify all 7 dimension variables
            Assert.Contains("float P =", content);   // Priority
            Assert.Contains("float Rs =", content);   // SceneRelevance
            Assert.Contains("float Q =", content);    // QuerySimilarity
            Assert.Contains("float Rc =", content);   // Recency
            Assert.Contains("float F =", content);    // UseFeedback
            Assert.Contains("float Pin =", content);   // UserPin
            Assert.Contains("float Cd =", content);   // CooldownPenalty
        }

        [Fact]
        public void BudgetSchedulerConfig_Contains_Weight_Fields()
        {
            var configPath = Path.Combine(
                RepoRoot, "RimMind-Core", "Source", "Application", "Common", "Models", "Context", "BudgetSchedulerConfig.cs");

            Assert.True(File.Exists(configPath), "BudgetSchedulerConfig.cs must exist");

            var content = File.ReadAllText(configPath);

            Assert.Contains("W1", content);
            Assert.Contains("W2", content);
            Assert.Contains("W3", content);
            Assert.Contains("W4", content);
            Assert.Contains("W5", content);
            Assert.Contains("W6", content);
            Assert.Contains("RecencyHalflife", content);
            Assert.Contains("CooldownWindow", content);
        }

        [Fact]
        public void ScoreKey_Uses_IRelevanceTable()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("IRelevanceTable", content);
            Assert.Contains("_relevanceTable", content);
            Assert.Contains("GetRelevance", content);
        }

        [Fact]
        public void ScoreKey_UserPin_Hard_Boost()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("1000f * Pin", content);
        }
    }
}
