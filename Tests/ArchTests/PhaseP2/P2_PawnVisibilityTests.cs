using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP2
{
    public class P2_PawnVisibilityTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private const string CompPawnAgentRelative = "Infrastructure/Verse/CompPawnAgent.cs";
        private const string WindowAgentStateDebugRelative = "Infrastructure/UI/Window_AgentStateDebug.cs";
        private const string WindowAgentModeDebugRelative = "Infrastructure/UI/Window_AgentModeDebug.cs";
        private const string WindowAgentFlowLabRelative = "Infrastructure/UI/Window_AgentFlowLab.cs";

        // --- Plan C: CompPawnAgent gizmo collapse ---
        // Overlapping assertions (Control key, PostSpawnSetup, EnsureAgentCreated) are covered by P7C.

        [Fact]
        public void CompPawnAgent_AgentControl_Uses_Independent_Agent_Icon()
        {
            var content = ReadSourceFile(CompPawnAgentRelative);
            Assert.Contains("AgentIcon", content);
        }

        // --- Window_AgentStateDebug (unchanged by Plan C) ---

        [Fact]
        public void Window_AgentStateDebug_Accepts_Pawn_Constructor()
        {
            var content = ReadSourceFile(WindowAgentStateDebugRelative);
            Assert.Contains("Window_AgentStateDebug(Pawn? pawn)", content);
            Assert.Contains("_targetPawn", content);
        }

        [Fact]
        public void Window_AgentStateDebug_Has_Parameterless_Constructor_Chain()
        {
            var content = ReadSourceFile(WindowAgentStateDebugRelative);
            Assert.Contains("Window_AgentStateDebug() : this(pawn: null, agent: null)", content);
        }

        [Fact]
        public void Window_AgentStateDebug_Uses_TargetPawn_With_Fallback()
        {
            var content = ReadSourceFile(WindowAgentStateDebugRelative);
            Assert.Contains("_targetPawn", content);
            Assert.Contains("Find.Selector.SingleSelectedThing", content);
        }

        // --- Window_AgentModeDebug (unchanged by Plan C) ---

        [Fact]
        public void Window_AgentModeDebug_Accepts_Pawn_Constructor()
        {
            var content = ReadSourceFile(WindowAgentModeDebugRelative);
            Assert.Contains("Window_AgentModeDebug(Pawn? pawn)", content);
            Assert.Contains("_initialPawn", content);
        }

        [Fact]
        public void Window_AgentModeDebug_Has_Parameterless_Constructor_Chain()
        {
            var content = ReadSourceFile(WindowAgentModeDebugRelative);
            Assert.Contains("Window_AgentModeDebug() : this(null)", content);
        }

        [Fact]
        public void Window_AgentModeDebug_Uses_InitialPawn_For_Selection()
        {
            var content = ReadSourceFile(WindowAgentModeDebugRelative);
            Assert.Contains("_initialPawn", content);
            Assert.Contains("_cachedPawns.IndexOf(_initialPawn)", content);
        }

        // --- Window_AgentFlowLab (unchanged by Plan C) ---

        [Fact]
        public void Window_AgentFlowLab_Accepts_Pawn_Constructor()
        {
            var content = ReadSourceFile(WindowAgentFlowLabRelative);
            Assert.Contains("Window_AgentFlowLab(Pawn? pawn)", content);
            Assert.Contains("_initialPawn", content);
        }

        [Fact]
        public void Window_AgentFlowLab_Has_Parameterless_Constructor_Chain()
        {
            var content = ReadSourceFile(WindowAgentFlowLabRelative);
            Assert.Contains("Window_AgentFlowLab() : this(null)", content);
        }

        [Fact]
        public void Window_AgentFlowLab_Uses_InitialPawn_With_Fallback()
        {
            var content = ReadSourceFile(WindowAgentFlowLabRelative);
            Assert.Contains("_initialPawn", content);
            Assert.Contains("Find.Selector.SingleSelectedThing", content);
        }
    }
}
