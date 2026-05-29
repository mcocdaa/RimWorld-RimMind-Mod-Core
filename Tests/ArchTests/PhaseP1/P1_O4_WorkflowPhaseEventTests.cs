using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-O4: TransitionWorkflow Event Publishing.
    /// Verifies that PawnAgent.TransitionWorkflow publishes an AgentBusEvent
    /// with AgentBusEventType.WorkflowPhaseChange, and that the enum value exists.
    /// </summary>
    public class P1_O4_WorkflowPhaseEventTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentDir = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Agent");

        private static readonly string DomainEventsDir = Path.Combine(
            ProjectRoot, "Source", "Domain", "Events");

        private static readonly string PawnAgentPath = Path.Combine(AgentDir, "PawnAgent.cs");
        private static readonly string AgentBusEventPath = Path.Combine(DomainEventsDir, "AgentBusEvent.cs");

        [Fact]
        public void AgentBusEventType_Contains_WorkflowPhaseChange()
        {
            var source = File.ReadAllText(AgentBusEventPath);
            Assert.Contains("WorkflowPhaseChange", source);
        }

        [Fact]
        public void TransitionWorkflow_Publishes_WorkflowPhaseChange_Event()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("WorkflowPhaseChange", source);
            Assert.Contains("AgentBusEventType.WorkflowPhaseChange", source);
        }

        [Fact]
        public void TransitionWorkflow_Calls_AgentBus_Publish()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("_agentBus?.Publish", source);
        }

        [Fact]
        public void TransitionWorkflow_Publishes_With_NpcId_And_ThingId()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("Identity.NpcId", source);
            Assert.Contains("Pawn?.thingIDNumber", source);
        }

        [Fact]
        public void TransitionWorkflow_Sets_WorkflowPhase_Before_Publish()
        {
            var source = File.ReadAllText(PawnAgentPath);
            // Verify the method sets _workflowPhase before publishing
            var lines = source.Split('\n');
            bool inTransitionWorkflow = false;
            int phaseSetLine = -1;
            int publishLine = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.Contains("TransitionWorkflow(AgentWorkflowPhase target)"))
                    inTransitionWorkflow = true;
                if (inTransitionWorkflow && trimmed.Contains("_workflowPhase = target"))
                    phaseSetLine = i;
                if (inTransitionWorkflow && trimmed.Contains("_agentBus?.Publish"))
                    publishLine = i;
                if (inTransitionWorkflow && trimmed == "}" && phaseSetLine > 0 && publishLine > 0)
                    break;
            }

            Assert.True(phaseSetLine > 0, "TransitionWorkflow should set _workflowPhase");
            Assert.True(publishLine > 0, "TransitionWorkflow should call _agentBus?.Publish");
            Assert.True(phaseSetLine < publishLine,
                "Phase should be set before publishing the event");
        }
    }
}
