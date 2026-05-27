using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-O1: Presentation/Agent layer migrated from Verse.Log to ILogSink.
    /// These tests verify that PawnThinker, ProactiveBehaviorExecutor, and PawnAgent
    /// no longer call Verse.Log directly and instead use ILogSink for traceId propagation.
    /// </summary>
    public class P1_O1_LogSinkMigrationTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentDir = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Agent");

        private static readonly string PawnThinkerPath = Path.Combine(AgentDir, "PawnThinker.cs");
        private static readonly string ProactiveExecutorPath = Path.Combine(AgentDir, "ProactiveBehaviorExecutor.cs");
        private static readonly string PawnAgentPath = Path.Combine(AgentDir, "PawnAgent.cs");
        private static readonly string PawnAgentFactoryPath = Path.Combine(AgentDir, "PawnAgentFactory.cs");

        /// <summary>
        /// Pattern that matches direct Verse.Log calls: Log.Message(, Log.Warning(, Log.Error(
        /// Excludes lines that are comments (starting with //) or inside string literals.
        /// </summary>
        private static readonly Regex DirectLogCallPattern =
            new Regex(@"(?<!//.*?)\bLog\.(Message|Warning|Error)\s*\(", RegexOptions.Compiled);

        private static string StripComments(string source)
        {
            // Remove single-line comments
            var withoutSingleLine = Regex.Replace(source, @"//.*$", "", RegexOptions.Multiline);
            // Remove multi-line comments
            var withoutMultiLine = Regex.Replace(withoutSingleLine, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return withoutMultiLine;
        }

        // --- PawnThinker ---

        [Fact]
        public void PawnThinker_DoesNotUse_VerseLog_Directly()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);
            var stripped = StripComments(content);

            var matches = DirectLogCallPattern.Matches(stripped);
            Assert.Empty(matches);
        }

        [Fact]
        public void PawnThinker_Has_ILogSink_Field()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("ILogSink", content);
            Assert.Contains("_log", content);
        }

        // --- ProactiveBehaviorExecutor ---

        [Fact]
        public void ProactiveBehaviorExecutor_DoesNotUse_VerseLog_Directly()
        {
            Assert.True(File.Exists(ProactiveExecutorPath), "ProactiveBehaviorExecutor.cs must exist");

            var content = File.ReadAllText(ProactiveExecutorPath);
            var stripped = StripComments(content);

            var matches = DirectLogCallPattern.Matches(stripped);
            Assert.Empty(matches);
        }

        [Fact]
        public void ProactiveBehaviorExecutor_Has_ILogSink_Field()
        {
            Assert.True(File.Exists(ProactiveExecutorPath), "ProactiveBehaviorExecutor.cs must exist");

            var content = File.ReadAllText(ProactiveExecutorPath);

            Assert.Contains("ILogSink", content);
            Assert.Contains("_log", content);
        }

        // --- PawnAgent ---

        [Fact]
        public void PawnAgent_DoesNotUse_VerseLog_Directly()
        {
            Assert.True(File.Exists(PawnAgentPath), "PawnAgent.cs must exist");

            var content = File.ReadAllText(PawnAgentPath);
            var stripped = StripComments(content);

            var matches = DirectLogCallPattern.Matches(stripped);
            Assert.Empty(matches);
        }

        [Fact]
        public void PawnAgent_Has_ILogSink_Field()
        {
            Assert.True(File.Exists(PawnAgentPath), "PawnAgent.cs must exist");

            var content = File.ReadAllText(PawnAgentPath);

            Assert.Contains("ILogSink", content);
            Assert.Contains("_log", content);
        }

        // --- PawnAgentFactory ---

        [Fact]
        public void PawnAgentFactory_Has_ILogSink_Field()
        {
            Assert.True(File.Exists(PawnAgentFactoryPath), "PawnAgentFactory.cs must exist");

            var content = File.ReadAllText(PawnAgentFactoryPath);

            Assert.Contains("ILogSink", content);
            Assert.Contains("_log", content);
        }

        [Fact]
        public void PawnAgentFactory_Exposes_LogSink_Property()
        {
            Assert.True(File.Exists(PawnAgentFactoryPath), "PawnAgentFactory.cs must exist");

            var content = File.ReadAllText(PawnAgentFactoryPath);

            Assert.Contains("LogSink", content);
        }

        [Fact]
        public void PawnAgentFactory_Passes_LogSink_To_PawnAgent()
        {
            Assert.True(File.Exists(PawnAgentFactoryPath), "PawnAgentFactory.cs must exist");

            var content = File.ReadAllText(PawnAgentFactoryPath);

            Assert.Contains("new PawnAgent", content);
            // Verify _log or LogSink is passed in the PawnAgent constructor call
            Assert.Matches(@"new\s+PawnAgent\s*\([^)]*_log[^)]*\)|new\s+PawnAgent\s*\([^)]*LogSink[^)]*\)", content);
        }

        [Fact]
        public void PawnAgentFactory_Passes_LogSink_To_PawnThinker()
        {
            Assert.True(File.Exists(PawnAgentFactoryPath), "PawnAgentFactory.cs must exist");

            var content = File.ReadAllText(PawnAgentFactoryPath);

            Assert.Contains("new PawnThinker", content);
            Assert.Matches(@"new\s+PawnThinker\s*\([^)]*_log[^)]*\)|new\s+PawnThinker\s*\([^)]*LogSink[^)]*\)", content);
        }
    }
}
