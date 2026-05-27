using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    /// <summary>
    /// R_N3: BudgetScheduler.GetFeedbackScore does NOT contain hardcoded 0.5f as the
    /// primary return value. It must delegate to _learner for feedback scoring,
    /// with 0.5f only allowed as a null-coalescing fallback.
    /// </summary>
    public class R_N3_BudgetSchedulerNoHardcodedFeedback
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string BudgetSchedulerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "BudgetScheduler.cs");

        [Fact]
        public void GetFeedbackScore_Uses_Learner()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("GetFeedbackScore", content);
            Assert.Contains("_learner", content);
        }

        [Fact]
        public void GetFeedbackScore_Delegates_To_Learner_GetFeedbackScore()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            Assert.Contains("_learner?.GetFeedbackScore", content);
        }

        [Fact]
        public void GetFeedbackScore_No_Standalone_Hardcoded_Half()
        {
            Assert.True(File.Exists(BudgetSchedulerPath), "BudgetScheduler.cs must exist");

            var content = File.ReadAllText(BudgetSchedulerPath);

            // The method must call _learner, not just return 0.5f
            Assert.Contains("_learner", content);

            // The GetFeedbackScore method must delegate to _learner
            Assert.Contains("_learner?.GetFeedbackScore", content);

            // If 0.5f appears in GetFeedbackScore context, it must be a null-coalescing fallback
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var inGetFeedbackScore = false;
            foreach (var line in lines)
            {
                if (line.Contains("private float GetFeedbackScore("))
                {
                    inGetFeedbackScore = true;
                    continue;
                }
                if (inGetFeedbackScore && line.Trim() == "}")
                {
                    inGetFeedbackScore = false;
                    continue;
                }
                if (inGetFeedbackScore && line.Contains("0.5f"))
                {
                    Assert.Contains("?? 0.5f", line);
                }
            }
        }
    }
}
