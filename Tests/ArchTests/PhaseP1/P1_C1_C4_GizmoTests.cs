using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-C1~C4: Originally tested individual gizmos (FloatMenu, EmergencyStop, Pause, ForceThink).
    /// Plan C collapsed all pawn gizmos into one "Agent Control" gizmo that opens the Debug Center.
    /// Overlapping assertions (Control key, DevMode guard, localization) are covered by P7C.
    /// These tests verify P1-specific concerns not covered by P7C.
    /// </summary>
    public class P1_C1_C4_GizmoTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string CompPawnAgentPath = Path.Combine(
            ProjectRoot, "Source", "Infrastructure", "Verse", "CompPawnAgent.cs");

        private static string ReadSource() => File.ReadAllText(CompPawnAgentPath);

        [Fact]
        public void CompPawnAgent_AgentControl_Opens_DebugCenter()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            Assert.Contains("Window_RimMindHub.OpenAgentsForPawn(Pawn)", content);
        }

        [Fact]
        public void CompPawnAgent_DoesNot_Have_OldIndividualGizmos()
        {
            Assert.True(File.Exists(CompPawnAgentPath), "CompPawnAgent.cs must exist");
            var content = ReadSource();
            // Old individual gizmos have been moved into Debug Center Agents page
            Assert.DoesNotContain("RimMind.Agent.Gizmo.EmergencyStop", content);
            Assert.DoesNotContain("RimMind.Agent.Gizmo.ForceThink", content);
            Assert.DoesNotContain("RimMind.Agent.Gizmo.Pause", content);
            Assert.DoesNotContain("RimMind.Agent.Gizmo.Resume", content);
            Assert.DoesNotContain("RimMind.Agent.Gizmo.CreateAgent", content);
            Assert.DoesNotContain("FloatMenu", content);
        }
    }
}
