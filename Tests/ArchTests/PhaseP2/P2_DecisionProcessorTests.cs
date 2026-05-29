using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP2
{
    /// <summary>
    /// P2: DecisionProcessor extraction from PawnThinker.
    /// Verifies that IDecisionProcessor and DecisionProcessor exist in Application layer,
    /// PawnThinker delegates to IDecisionProcessor, and PawnThinker stays under 150 LOC.
    /// </summary>
    public class P2_DecisionProcessorTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath));

        [Fact]
        public void IDecisionProcessor_Exists_In_Application_Features()
        {
            var path = Path.Combine(SourceDir, "Application", "Features", "Agent", "IDecisionProcessor.cs");
            Assert.True(File.Exists(path), "IDecisionProcessor.cs should exist in Application/Features/Agent");
            var content = ReadSourceFile("Application/Features/Agent/IDecisionProcessor.cs");
            Assert.Contains("IDecisionProcessor", content);
        }

        [Fact]
        public void IDecisionProcessor_Has_ProcessResult_Method()
        {
            var content = ReadSourceFile("Application/Features/Agent/IDecisionProcessor.cs");
            Assert.Contains("ProcessResult", content);
        }

        [Fact]
        public void DecisionProcessor_Exists_In_Application_Features()
        {
            var path = Path.Combine(SourceDir, "Application", "Features", "Agent", "DecisionProcessor.cs");
            Assert.True(File.Exists(path), "DecisionProcessor.cs should exist in Application/Features/Agent");
            var content = ReadSourceFile("Application/Features/Agent/DecisionProcessor.cs");
            Assert.Contains("DecisionProcessor", content);
        }

        [Fact]
        public void DecisionProcessor_Implements_IDecisionProcessor()
        {
            var content = ReadSourceFile("Application/Features/Agent/DecisionProcessor.cs");
            Assert.Contains("IDecisionProcessor", content);
        }

        [Fact]
        public void DecisionProcessor_Handles_Agentic_Loop()
        {
            var content = ReadSourceFile("Application/Features/Agent/DecisionProcessor.cs");
            Assert.Contains("IAgenticLoopService", content);
        }

        [Fact]
        public void DecisionProcessor_Handles_DecisionFailedEvent()
        {
            var content = ReadSourceFile("Application/Features/Agent/DecisionProcessor.cs");
            Assert.Contains("DecisionFailedEvent", content);
        }

        [Fact]
        public void DecisionProcessor_Records_Behavior()
        {
            var content = ReadSourceFile("Application/Features/Agent/DecisionProcessor.cs");
            Assert.Contains("RecordBehavior", content);
        }

        [Fact]
        public void PawnThinker_Delegates_To_DecisionProcessor()
        {
            var content = ReadSourceFile("Presentation/Agent/PawnThinker.cs");
            Assert.Contains("IDecisionProcessor", content);
        }

        [Fact]
        public void PawnThinker_Does_Not_Contain_ParseDecision()
        {
            var content = ReadSourceFile("Presentation/Agent/PawnThinker.cs");
            Assert.DoesNotContain("strategy.ParseDecision", content);
        }

        [Fact]
        public void PawnThinker_Is_Under_150_NonEmpty_Lines()
        {
            var content = ReadSourceFile("Presentation/Agent/PawnThinker.cs");
            var lineCount = CountNonEmptyLines(content);
            Assert.True(lineCount <= 150, $"PawnThinker has {lineCount} non-empty lines, exceeds 150 LOC limit");
        }

        private static int CountNonEmptyLines(string content)
        {
            int count = 0;
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && trimmed != "{" && trimmed != "}")
                    count++;
            }
            return count;
        }
    }
}
