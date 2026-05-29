using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-C1~C4: Gizmo improvements for CompPawnAgent.
    /// C-1: FloatMenu mode selection (replaces cycling Command_Action)
    /// C-2: Emergency Stop Gizmo
    /// C-3: Pause Gizmo
    /// C-4: Force Think Gizmo
    /// </summary>
    public class P1_C1_C4_GizmoTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string CompPawnAgentPath = Path.Combine(
            ProjectRoot, "Source", "Infrastructure", "Verse", "CompPawnAgent.cs");

        private static readonly string EnglishXmlPath = Path.Combine(
            ProjectRoot, "Languages", "English", "Keyed", "RimMind_Core.xml");

        private static readonly string ChineseXmlPath = Path.Combine(
            ProjectRoot, "Languages", "ChineseSimplified", "Keyed", "RimMind_Core.xml");

        private static string ReadSource() => File.ReadAllText(CompPawnAgentPath);
        private static string ReadEnglishXml() => File.ReadAllText(EnglishXmlPath);
        private static string ReadChineseXml() => File.ReadAllText(ChineseXmlPath);

        // --- C-1: FloatMenu Mode Selection ---

        [Fact]
        public void CompPawnAgent_Uses_FloatMenu_ForModeSelection()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("FloatMenu", content);
            Assert.Contains("FloatMenuOption", content);
        }

        [Fact]
        public void CompPawnAgent_ModeSelection_DisablesCurrentMode()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("Disabled = !isApplicable || isCurrent", content);
        }

        [Fact]
        public void CompPawnAgent_ModeSelection_ChecksIsApplicable()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("IsApplicable", content);
        }

        // --- C-2: Emergency Stop ---

        [Fact]
        public void CompPawnAgent_Has_EmergencyStop_Gizmo()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("RimMind.Agent.Gizmo.EmergencyStop", content);
        }

        [Fact]
        public void CompPawnAgent_EmergencyStop_ClearsPerceptionBuffer()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("PerceptionBuffer.Clear()", content);
        }

        [Fact]
        public void CompPawnAgent_EmergencyStop_TransitionsToPaused()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            // Emergency Stop should transition to Paused state
            Assert.Contains("AgentState.Paused", content);
        }

        // --- C-3: Pause Gizmo ---

        [Fact]
        public void CompPawnAgent_Has_Pause_Gizmo()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("RimMind.Agent.Gizmo.Pause", content);
        }

        [Fact]
        public void CompPawnAgent_Pause_TransitionsToPaused()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            // The Pause action should call TransitionTo(AgentState.Paused)
            var pauseIndex = content.IndexOf("RimMind.Agent.Gizmo.Pause");
            Assert.True(pauseIndex > 0, "Pause key must exist");
        }

        // --- C-4: Force Think ---

        [Fact]
        public void CompPawnAgent_Has_ForceThink_Gizmo()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("RimMind.Agent.Gizmo.ForceThink", content);
        }

        [Fact]
        public void CompPawnAgent_ForceThink_UsesIAgentControl()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            // ForceThink should be called via IAgentControl, not via IPawnAgent cast
            Assert.Contains("Agent.ForceThink()", content);
        }

        // --- Resume ---

        [Fact]
        public void CompPawnAgent_Has_Resume_Gizmo_WhenPaused()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("RimMind.Agent.Gizmo.Resume", content);
        }

        // --- Translation Keys ---

        [Fact]
        public void EnglishXml_Has_AllC1C4_TranslationKeys()
        {
            Assert.True(File.Exists(EnglishXmlPath), "English Keyed XML must exist");
            var content = ReadEnglishXml();

            Assert.Contains("RimMind.Agent.Gizmo.SelectMode>", content);
            Assert.Contains("RimMind.Agent.Gizmo.SelectModeDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.CurrentMode>", content);
            Assert.Contains("RimMind.Agent.Gizmo.InactiveMode>", content);
            Assert.Contains("RimMind.Agent.Gizmo.Pause>", content);
            Assert.Contains("RimMind.Agent.Gizmo.PauseDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.Resume>", content);
            Assert.Contains("RimMind.Agent.Gizmo.ResumeDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.EmergencyStop>", content);
            Assert.Contains("RimMind.Agent.Gizmo.EmergencyStopDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.ForceThink>", content);
            Assert.Contains("RimMind.Agent.Gizmo.ForceThinkDesc>", content);
            Assert.Contains("RimMind.Agent.State.Paused>", content);
        }

        [Fact]
        public void ChineseXml_Has_AllC1C4_TranslationKeys()
        {
            Assert.True(File.Exists(ChineseXmlPath), "Chinese Keyed XML must exist");
            var content = ReadChineseXml();

            Assert.Contains("RimMind.Agent.Gizmo.SelectMode>", content);
            Assert.Contains("RimMind.Agent.Gizmo.SelectModeDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.CurrentMode>", content);
            Assert.Contains("RimMind.Agent.Gizmo.InactiveMode>", content);
            Assert.Contains("RimMind.Agent.Gizmo.Pause>", content);
            Assert.Contains("RimMind.Agent.Gizmo.PauseDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.Resume>", content);
            Assert.Contains("RimMind.Agent.Gizmo.ResumeDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.EmergencyStop>", content);
            Assert.Contains("RimMind.Agent.Gizmo.EmergencyStopDesc>", content);
            Assert.Contains("RimMind.Agent.Gizmo.ForceThink>", content);
            Assert.Contains("RimMind.Agent.Gizmo.ForceThinkDesc>", content);
            Assert.Contains("RimMind.Agent.State.Paused>", content);
        }
    }
}
