using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L5_DiffTrackerSplit
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string DiffDir = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "Diff");

        private static readonly string DiffTrackerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "ContextDiffTracker.cs");

        [Fact]
        public void Diff_Subdirectory_Contains_Three_Files()
        {
            Assert.True(Directory.Exists(DiffDir), "Diff/ subdirectory must exist");

            var expectedFiles = new[] { "DiffComputer.cs", "DiffRecorder.cs", "DiffMerger.cs" };
            foreach (var fileName in expectedFiles)
            {
                var fullPath = Path.Combine(DiffDir, fileName);
                Assert.True(File.Exists(fullPath), $"Diff/ must contain {fileName}");
            }
        }

        [Fact]
        public void ContextDiffTracker_Less_Than_Or_Equal_100_LOC()
        {
            Assert.True(File.Exists(DiffTrackerPath), "ContextDiffTracker.cs must exist");

            var lines = File.ReadAllLines(DiffTrackerPath)
                .Where(l => !string.IsNullOrWhiteSpace(l)
                         && !l.TrimStart().StartsWith("//")
                         && !l.TrimStart().StartsWith("///"))
                .ToList();

            Assert.True(lines.Count <= 100,
                $"ContextDiffTracker.cs should be <= 100 LOC (excluding blanks/comments), found {lines.Count}");
        }

        [Fact]
        public void ContextDiffTracker_Delegates_To_Sub_Components()
        {
            Assert.True(File.Exists(DiffTrackerPath), "ContextDiffTracker.cs must exist");

            var content = File.ReadAllText(DiffTrackerPath);

            Assert.Contains("DiffComputer", content);
            Assert.Contains("DiffRecorder", content);
            Assert.Contains("DiffMerger", content);
        }
    }
}
