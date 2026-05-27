using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L9_DiffMergerMergeExpiredNonEmpty
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string DiffMergerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "Diff", "DiffMerger.cs");

        private static readonly string DiffTrackerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "ContextDiffTracker.cs");

        [Fact]
        public void DiffMerger_MergeExpired_MethodBody_NonEmpty()
        {
            Assert.True(File.Exists(DiffMergerPath), "DiffMerger.cs must exist");

            var content = File.ReadAllText(DiffMergerPath);

            Assert.Contains("MergeExpired", content);

            // Verify method body is not just a placeholder/empty
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inMethod = false;
            int bodyLines = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("MergeExpired") && trimmed.Contains("("))
                    inMethod = true;

                if (inMethod)
                {
                    if (trimmed == "{" || trimmed == "}" || string.IsNullOrWhiteSpace(trimmed))
                    {
                        if (trimmed == "}")
                            break;
                        continue;
                    }
                    bodyLines++;
                }
            }

            Assert.True(bodyLines > 5,
                $"MergeExpired method body should have > 5 LOC, found {bodyLines}");
        }

        [Fact]
        public void DiffMerger_MergeExpired_Uses_CacheManager()
        {
            Assert.True(File.Exists(DiffMergerPath), "DiffMerger.cs must exist");

            var content = File.ReadAllText(DiffMergerPath);

            Assert.Contains("cacheManager", content);
        }

        [Fact]
        public void ContextDiffTracker_UpdateKeyValues_MethodBody_NonEmpty()
        {
            Assert.True(File.Exists(DiffTrackerPath), "ContextDiffTracker.cs must exist");

            var content = File.ReadAllText(DiffTrackerPath);

            Assert.Contains("UpdateKeyValues", content);

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inMethod = false;
            int bodyLines = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("UpdateKeyValues") && trimmed.Contains("("))
                    inMethod = true;

                if (inMethod)
                {
                    if (trimmed == "{" || trimmed == "}" || string.IsNullOrWhiteSpace(trimmed))
                    {
                        if (trimmed == "}")
                            break;
                        continue;
                    }
                    bodyLines++;
                }
            }

            Assert.True(bodyLines > 5,
                $"UpdateKeyValues method body should have > 5 LOC, found {bodyLines}");
        }

        [Fact]
        public void RelevanceLearner_Has_MaybeCleanup()
        {
            var learnerPath = Path.Combine(
                RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "RelevanceLearner.cs");

            Assert.True(File.Exists(learnerPath), "RelevanceLearner.cs must exist");

            var content = File.ReadAllText(learnerPath);

            Assert.Contains("MaybeCleanup", content);
            Assert.Contains("TimestampTick", content);
            Assert.Contains("36000", content);
        }
    }
}
